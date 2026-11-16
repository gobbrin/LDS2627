using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Participacoes;

/// <summary>
/// Gere a submissão, a simulação, a aceitação, a rejeição e o cancelamento de participações.
/// Utiliza abstrações de persistência, sem depender do EF Core.
/// </summary>
/// <remarks>
/// Padrão comum a aceitar, rejeitar e cancelar: (1) carregar a participação e a viagem (rastreadas); (2) verificar a
/// permissão e o estado; (3) alterar as entidades, registar as notificações e gravar tudo de uma vez com
/// <see cref="IUnidadeDeTrabalho.ConfirmarAsync"/> (DC-37); (4) só depois enviar o evento em tempo real e o push, que
/// já não dependem do cliente: a operação aconteceu, mesmo que o pedido HTTP seja cancelado entretanto.
/// As transições estão nas entidades (AD-001). Os lugares são verificados contra a contagem real de aceites;
/// se não baterem, a operação é recusada, para não agravar o erro.
/// As colunas <c>Versao</c> da viagem e da participação recusam a segunda de duas decisões simultâneas (DO-03).
/// </remarks>
public sealed class ParticipacaoService(
    IRepositorioParticipacoes repositorioParticipacoes,
    IRepositorioViagens repositorioViagens,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    INotificacaoService notificacaoService,
    INotificadorPedidosTempoReal notificadorPedidosTempoReal,
    IRotaService rotaService,
    IServicoMapasRotas mapas) : IParticipacaoService
{
    private const string MensagemPedidoRepetido = "Já existe um pedido ou participação ativa nesta viagem.";

    // Uma mensagem por regra dos pontos do pedido (BE-15).
    private static readonly Erro RecolhaIgualAoDestino = new(TipoErro.DadosInvalidos, "A recolha e o destino não podem ser o mesmo local.");
    private static readonly Erro RecolhaNoDestinoDaViagem = new(TipoErro.DadosInvalidos, "A recolha não pode ser o destino da viagem.");
    private static readonly Erro DestinoNaOrigemDaViagem = new(TipoErro.DadosInvalidos, "O destino não pode ser a origem da viagem.");
    private static readonly Erro ViagemIndisponivel = new(TipoErro.Conflito, "Viagem indisponível.");
    private static readonly Erro NaoEncontrada = new(TipoErro.NaoEncontrado, "Pedido não encontrado.");
    private static readonly Erro Proibido = new(TipoErro.Proibido, "Não tens permissão para gerir este pedido.");
    private static readonly Erro EstadoNaoPermite = new(TipoErro.Conflito, "O estado atual do pedido não permite esta operação.");
    private static readonly Erro SemCapacidade = new(TipoErro.Conflito, "Sem lugares disponíveis.");
    private static readonly Erro LugaresInconsistentes = new(TipoErro.Conflito, "A capacidade da viagem está inconsistente.");
    private static readonly Erro RotaIndisponivel = new(
        TipoErro.ServicoIndisponivel,
        "Serviço de mapas indisponível: não foi possível recalcular a rota.");

    // Coordenadas dos pontos indicados no pedido; um ponto omitido fica a null.
    private sealed record PontosDoPedido(Coordenadas? Recolha, Coordenadas? Destino);

    /// <summary>Valida o texto dos pontos indicados no pedido (P-20), sem consultar a BD nem os mapas.</summary>
    /// <param name="recolha">Ponto de recolha, ou <c>null</c> para a origem da viagem.</param>
    /// <param name="destino">Ponto de destino, ou <c>null</c> para o destino da viagem.</param>
    /// <returns>O erro da primeira regra que falha, com uma mensagem própria; <c>null</c> se estiver tudo certo.</returns>
    private static Erro? ValidarTextoDosPontos(string? recolha, string? destino)
    {
        // P-20: pelo menos um dos dois pontos.
        if (recolha is null && destino is null)
            return new Erro(TipoErro.DadosInvalidos, "Indica um ponto de recolha ou de destino.");

        if (recolha is not null && string.IsNullOrWhiteSpace(recolha))
            return new Erro(TipoErro.DadosInvalidos, "O ponto de recolha está vazio.");

        if (recolha is not null && recolha.Length > Participacao.TamanhoMaximoPonto)
            return new Erro(TipoErro.DadosInvalidos, "O ponto de recolha tem no máximo 200 caracteres.");

        if (destino is not null && string.IsNullOrWhiteSpace(destino))
            return new Erro(TipoErro.DadosInvalidos, "O ponto de destino está vazio.");

        if (destino is not null && destino.Length > Participacao.TamanhoMaximoPonto)
            return new Erro(TipoErro.DadosInvalidos, "O ponto de destino tem no máximo 200 caracteres.");

        return null;
    }

    /// <summary>
    /// Regras de mesmo local pelo texto: RF-F004-20 (recolha no destino da viagem), RF-F004-21 (destino na origem da
    /// viagem) e RF-F004-18 (recolha e destino efetivos). As duas primeiras vêm antes por terem a mensagem mais precisa:
    /// com um ponto omitido, a regra 18 também falharia.
    /// </summary>
    /// <param name="recolha">Recolha efetiva: a indicada ou, se omitida, a origem da viagem.</param>
    /// <param name="destino">Destino efetivo: o indicado ou, se omitido, o destino da viagem.</param>
    /// <param name="viagem">Viagem pedida.</param>
    /// <returns>O erro da regra que falha; <c>null</c> se nenhuma falhar.</returns>
    private static Erro? ValidarMesmoLocalPorTexto(string recolha, string destino, Viagem viagem)
    {
        if (RegraMesmoLocal.MesmoTexto(recolha, viagem.Destino))
            return RecolhaNoDestinoDaViagem;

        if (RegraMesmoLocal.MesmoTexto(destino, viagem.Origem))
            return DestinoNaOrigemDaViagem;

        if (RegraMesmoLocal.MesmoTexto(recolha, destino))
            return RecolhaIgualAoDestino;

        return null;
    }

    /// <summary>
    /// As mesmas regras de <see cref="ValidarMesmoLocalPorTexto"/>, agora pelas coordenadas. Um ponto omitido usa as
    /// coordenadas da viagem (P-20).
    /// </summary>
    /// <param name="viagem">Viagem pedida.</param>
    /// <param name="pontos">Coordenadas dos pontos indicados no pedido.</param>
    /// <returns>O erro da regra que falha; <c>null</c> se nenhuma falhar.</returns>
    private static Erro? ValidarMesmoLocalPorCoordenadas(Viagem viagem, PontosDoPedido pontos)
    {
        Coordenadas origemViagem = new(viagem.OrigemLatitude, viagem.OrigemLongitude);
        Coordenadas destinoViagem = new(viagem.DestinoLatitude, viagem.DestinoLongitude);
        Coordenadas recolha = pontos.Recolha ?? origemViagem;
        Coordenadas destino = pontos.Destino ?? destinoViagem;

        if (MesmasCoordenadas(recolha, destinoViagem))
            return RecolhaNoDestinoDaViagem;

        if (MesmasCoordenadas(destino, origemViagem))
            return DestinoNaOrigemDaViagem;

        if (MesmasCoordenadas(recolha, destino))
            return RecolhaIgualAoDestino;

        return null;
    }

    private static bool MesmasCoordenadas(Coordenadas a, Coordenadas b) =>
        RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    /// <inheritdoc />
    /// <remarks>
    /// Passos, por ordem (BE-05): (1) validar o texto dos pontos, sem BD; (2) validar a viagem; (3) mesmo local pelo
    /// texto; (4) obter as coordenadas; (5) confirmar que a viagem continua ativa; (6) mesmo local pelas coordenadas;
    /// (7) criar, gravar e avisar o condutor.
    /// </remarks>
    public async Task<Resultado<PedidoCriado>> CriarPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        // 1. P-20: os pontos validam-se primeiro, porque não precisam da BD.
        if (ValidarTextoDosPontos(recolha, destino) is { } pontosInvalidos)
            return pontosInvalidos;

        // 2. A viagem tem de existir e aceitar um pedido deste passageiro.
        Viagem? viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        if (await ValidarViagemParaPedidoAsync(viagem, passageiroId, ct) is { } viagemRecusada)
            return viagemRecusada;

        // 3. Primeiro só pelo texto, para não chamar o Serviço de Mapas sem necessidade.
        if (ValidarMesmoLocalPorTexto(recolha ?? viagem.Origem, destino ?? viagem.Destino, viagem) is { } mesmoTexto)
            return mesmoTexto;

        // 4. Coordenadas dos pontos indicados.
        Resultado<PontosDoPedido> pontos = await ObterCoordenadasAsync(viagem, recolha, destino, ct);

        if (pontos.Erro is { } erroMapas)
            return erroMapas;

        // 5. Os mapas podem demorar até 10 s e a gravação do pedido não toca na linha da viagem, por isso Versao não
        // deteta a mudança. Sem isto, o pedido ficaria Pendente para sempre numa viagem já terminada.
        if (await repositorioViagens.ObterEstadoAsync(viagem.Id, ct) != EstadoViagem.Ativa)
            return ViagemIndisponivel;

        // 6. As mesmas regras de mesmo local, agora pelas coordenadas.
        if (ValidarMesmoLocalPorCoordenadas(viagem, pontos.Valor) is { } mesmasCoordenadas)
            return mesmasCoordenadas;

        // 7. O pedido fica pendente, sem descontar lugares.
        Participacao pedido = Participacao.Pedir(
            viagem.Id,
            passageiroId,
            recolha,
            pontos.Valor.Recolha,
            destino,
            pontos.Valor.Destino,
            DateTime.UtcNow);

        return await GravarENotificarPedidoAsync(viagem, pedido, ct);
    }

    /// <summary>
    /// Verifica se a viagem aceita um pedido deste passageiro: ativa e por partir, não é do próprio passageiro
    /// (RF-F004-05), sem outro pedido ativo dele (RF-F004-06) e com lugar livre.
    /// </summary>
    /// <returns>O erro da primeira regra que falha; <c>null</c> se a viagem aceitar o pedido.</returns>
    private async Task<Erro?> ValidarViagemParaPedidoAsync(Viagem viagem, Guid passageiroId, CancellationToken ct)
    {
        if (!viagem.PorPartir(DateTime.UtcNow))
            return ViagemIndisponivel;

        if (viagem.CondutorId == passageiroId)
            return new Erro(TipoErro.Conflito, "Não podes pedir participação na tua própria viagem.");

        if (await repositorioParticipacoes.ExisteAtivaAsync(viagem.Id, passageiroId, ct))
            return new Erro(TipoErro.Conflito, MensagemPedidoRepetido);

        // Só as participações aceites ocupam lugares; a regra é a da viagem (BE-16).
        if (!viagem.TemLugarLivre)
            return new Erro(TipoErro.Conflito, "Não existem lugares disponíveis nesta viagem.");

        return null;
    }

    /// <summary>
    /// Obtém as coordenadas dos pontos indicados no pedido (RF-F008-01). Um ponto omitido fica a <c>null</c> (P-20).
    /// </summary>
    /// <remarks>
    /// Um ponto com o mesmo texto que a origem ou o destino da viagem usa as coordenadas já guardadas. Só um ponto novo
    /// chama o Serviço de Mapas: sem pontos novos, o pedido não depende dele (P-19, BE-03).
    /// </remarks>
    /// <returns>As coordenadas, ou o erro dos mapas (422 ponto não encontrado, 502, 503, 504).</returns>
    private async Task<Resultado<PontosDoPedido>> ObterCoordenadasAsync(
        Viagem viagem,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        Coordenadas? coordenadasRecolha = CoordenadasJaConhecidas(viagem, recolha);
        Coordenadas? coordenadasDestino = CoordenadasJaConhecidas(viagem, destino);
        bool geocodificarRecolha = recolha is not null && coordenadasRecolha is null;
        bool geocodificarDestino = destino is not null && coordenadasDestino is null;

        if (!geocodificarRecolha && !geocodificarDestino)
            return new PontosDoPedido(coordenadasRecolha, coordenadasDestino);

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<PontosDoPedido>(
            async limite =>
            {
                if (geocodificarRecolha)
                {
                    coordenadasRecolha = await mapas.GeocodificarAsync(recolha!.Trim(), limite);

                    if (coordenadasRecolha is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de recolha não encontrado.");
                }

                if (geocodificarDestino)
                {
                    coordenadasDestino = await mapas.GeocodificarAsync(destino!.Trim(), limite);

                    if (coordenadasDestino is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de destino não encontrado.");
                }

                return new PontosDoPedido(coordenadasRecolha, coordenadasDestino);
            },
            "Serviço de mapas indisponível.",
            "Tempo de resposta do serviço de mapas excedido.",
            ct);
    }

    /// <summary>Coordenadas guardadas da viagem para um texto igual à sua origem ou ao seu destino.</summary>
    /// <returns>As coordenadas; <c>null</c> para um texto omitido ou que é um local novo.</returns>
    private static Coordenadas? CoordenadasJaConhecidas(Viagem viagem, string? texto)
    {
        if (texto is null)
            return null;

        if (RegraMesmoLocal.MesmoTexto(texto, viagem.Origem))
            return new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude);

        if (RegraMesmoLocal.MesmoTexto(texto, viagem.Destino))
            return new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude);

        return null;
    }

    /// <summary>
    /// Grava o pedido com a notificação do condutor e, só depois, envia o push e o evento em tempo real (RF-F006-01).
    /// </summary>
    /// <returns>O pedido criado; 409 se outro pedido simultâneo do mesmo passageiro ganhou (índice único da BD).</returns>
    private async Task<Resultado<PedidoCriado>> GravarENotificarPedidoAsync(
        Viagem viagem,
        Participacao pedido,
        CancellationToken ct)
    {
        Notificacao notificacao = notificacaoService.Registar(
            viagem.CondutorId,
            TipoNotificacao.PedidoRecebido,
            "Recebeste um novo pedido de participação numa viagem.",
            viagem.Id);

        // Grava o pedido e a notificação. Dois pedidos simultâneos podem passar a verificação acima; a BD recusa o segundo.
        if (!await repositorioParticipacoes.AdicionarPedidoAsync(pedido, ct))
            return new Erro(TipoErro.Conflito, MensagemPedidoRepetido);

        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);
        await notificadorPedidosTempoReal.PedidoRecebidoAsync(viagem.CondutorId, pedido.Id, CancellationToken.None);

        return new PedidoCriado(
            pedido.Id,
            pedido.ViagemId,
            pedido.PassageiroId,
            pedido.Estado.ToString(),
            pedido.Recolha,
            pedido.Destino,
            pedido.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<Resultado<SimulacaoPedido>> SimularAsync(
        Guid viagemId,
        Guid pedidoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        if (ValidarViagemParaSimular(viagem, utilizadorId) is { } erroViagem)
            return erroViagem;

        var pedido = await repositorioParticipacoes.ObterPorIdAsync(pedidoId, ct);

        if (pedido is null || pedido.ViagemId != viagemId)
            return new Erro(TipoErro.NaoEncontrado, "Pedido não encontrado nesta viagem.");

        if (ValidarPedidoParaSimular(viagem, pedido) is { } erroPedido)
            return erroPedido;

        var recolha = string.IsNullOrWhiteSpace(pedido.Recolha) ? viagem.Origem : pedido.Recolha.Trim();
        var destino = string.IsNullOrWhiteSpace(pedido.Destino) ? viagem.Destino : pedido.Destino.Trim();

        if (RegraMesmoLocal.MesmoTexto(recolha, destino))
            return new Erro(TipoErro.DadosInvalidos, "A recolha e o destino efetivos não podem ser iguais.");

        // Sem paragens novas, a rota simulada é a guardada: responde sem chamar os mapas (decisão de 16/11, BE-03).
        ResultadoRotaComPontos? guardada = RotaGuardada.Ler(viagem);

        if (guardada is not null && !RotaService.AcrescentaParagens(viagem, pedido))
            return MontarSimulacao(viagem, pedido, recolha, destino, guardada, guardada);

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        const string MensagemTempo = "O serviço de mapas excedeu o limite de 10 segundos.";

        return await ChamadaMapas.ExecutarAsync<SimulacaoPedido>(
            async limite =>
            {
                // A rota atual é a guardada; só falta calculá-la se estiver por recalcular (P-19). Nada é gravado.
                var original = guardada ?? await rotaService.CalcularAsync(viagem, aceites, limite);
                var simulada = await rotaService.CalcularAsync(viagem, [.. aceites, pedido], limite);

                if (original is null || simulada is null)
                    return new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

                return MontarSimulacao(viagem, pedido, recolha, destino, original, simulada);
            },
            "Falha na comunicação com o serviço de mapas.",
            MensagemTempo,
            ct);
    }

    /// <summary>Verifica a viagem antes de simular: é do condutor que pede e está ativa.</summary>
    /// <param name="viagem">Viagem da simulação.</param>
    /// <param name="utilizadorId">Utilizador que pede a simulação.</param>
    /// <returns>O erro a devolver, ou <c>null</c> se a viagem permite simular.</returns>
    private static Erro? ValidarViagemParaSimular(Viagem viagem, Guid utilizadorId)
    {
        if (viagem.CondutorId != utilizadorId)
            return new Erro(TipoErro.Proibido, "Só o condutor da viagem pode simular este pedido.");

        // A simulação antecede a decisão (RF-F004-09), por isso segue a mesma regra da aceitação (RF-F004-10).
        if (!viagem.EstaAtiva)
            return new Erro(TipoErro.Conflito, "A viagem não está ativa.");

        return null;
    }

    /// <summary>Verifica o pedido antes de simular: está pendente, tem pontos e há lugar na viagem (BE-26).</summary>
    /// <param name="viagem">Viagem já validada.</param>
    /// <param name="pedido">Pedido desta viagem.</param>
    /// <returns>O erro a devolver, ou <c>null</c> se o pedido pode ser simulado.</returns>
    private static Erro? ValidarPedidoParaSimular(Viagem viagem, Participacao pedido)
    {
        if (!pedido.EstaPendente)
            return new Erro(TipoErro.Conflito, "Só é possível simular pedidos pendentes.");

        if (string.IsNullOrWhiteSpace(pedido.Recolha) && string.IsNullOrWhiteSpace(pedido.Destino))
            return new Erro(TipoErro.DadosInvalidos, "O pedido deve indicar recolha ou destino.");

        // Numa viagem cheia o pedido não pode ser aceite, por isso não há nada a simular (BE-26).
        if (!viagem.TemLugarLivre)
            return SemCapacidade;

        return null;
    }

    /// <summary>Monta a resposta da simulação: a rota atual, a rota com o pedido e as diferenças (RF-F004-09).</summary>
    /// <param name="recolha">Recolha efetiva (a do pedido, ou a origem da viagem se for omitida).</param>
    /// <param name="destino">Destino efetivo (o do pedido, ou o destino da viagem se for omitido).</param>
    /// <param name="original">Rota atual da viagem.</param>
    /// <param name="simulada">Rota se o pedido fosse aceite.</param>
    private static SimulacaoPedido MontarSimulacao(
        Viagem viagem,
        Participacao pedido,
        string recolha,
        string destino,
        ResultadoRotaComPontos original,
        ResultadoRotaComPontos simulada)
    {
        return new SimulacaoPedido(
            viagem.Id,
            pedido.Id,
            viagem.Origem,
            viagem.Destino,
            recolha,
            destino,
            original.DistanciaTotalKm,
            original.DuracaoTotalMinutos,
            simulada.DistanciaTotalKm,
            simulada.DuracaoTotalMinutos,
            Math.Round(simulada.DistanciaTotalKm - original.DistanciaTotalKm, 2),
            simulada.DuracaoTotalMinutos - original.DuracaoTotalMinutos,
            original.PontosOrdenados.Select(p => p.Local).ToList(),
            simulada.PontosOrdenados.Select(p => p.Local).ToList());
    }

    /// <inheritdoc />
    public async Task<Resultado> AceitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return Proibido;

        // RF-F004-10 / AD-001, regra 3 (F5): só pedidos pendentes de viagens ativas, mesmo depois da hora de partida.
        if (!participacao.EstaPendente || !viagem.EstaAtiva)
            return EstadoNaoPermite;

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
            viagem.Id,
            EstadoParticipacao.Aceite,
            ct);

        // RF-F002-13: lugares disponíveis = oferecidos - aceites. O valor guardado tem de bater com a contagem.
        if (!viagem.LugaresConsistentes(aceites.Count))
            return LugaresInconsistentes;

        if (!viagem.TemLugarLivre)
            return SemCapacidade;

        // RF-F008-05 / P-19: um pedido com paragens novas recalcula a rota; sem o Serviço de Mapas é recusado e nada
        // muda. Sem paragens novas a rota guardada continua certa e não se chamam os mapas (decisão de 16/11, BE-03).
        if (RotaService.AcrescentaParagens(viagem, participacao))
        {
            bool rotaAtualizada = await rotaService.RecalcularAsync(viagem, [.. aceites, participacao], ct);

            if (!rotaAtualizada)
                return RotaIndisponivel;
        }

        participacao.Aceitar(DateTime.UtcNow);
        viagem.OcuparLugar();

        var notificacao = notificacaoService.Registar(
            participacao.PassageiroId,
            TipoNotificacao.PedidoAceite,
            "O teu pedido de participação na viagem foi aceite.",
            viagem.Id);

        // RF-F006-14 / P-23: o novo passageiro recebe Pedido aceite e não Alteração da viagem pela sua entrada.
        notificacaoService.RegistarAlteracaoViagem(
            viagem.Id,
            viagem.CondutorId,
            aceites.Select(p => p.PassageiroId),
            utilizadorId,
            "Um novo passageiro foi aceite na viagem em que participas.");

        // Um conflito nas colunas Versao chega ao cliente como 409, pela unidade de trabalho.
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        await notificadorPedidosTempoReal.PedidoAceiteAsync(participacao.PassageiroId, participacao.Id, CancellationToken.None);
        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> RejeitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return Proibido;

        if (!participacao.EstaPendente)
            return EstadoNaoPermite;

        participacao.Rejeitar();

        var notificacao = notificacaoService.Registar(
            participacao.PassageiroId,
            TipoNotificacao.PedidoRejeitado,
            "O teu pedido de participação na viagem foi rejeitado.",
            viagem.Id);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        await notificadorPedidosTempoReal.PedidoRejeitadoAsync(participacao.PassageiroId, participacao.Id, CancellationToken.None);
        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> CancelarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        // AD-001 (F1, DC-35): só o passageiro cancela; o condutor rejeita pendentes ou indisponibiliza a viagem.
        if (participacao.PassageiroId != utilizadorId)
            return Proibido;

        var agora = DateTime.UtcNow;

        // Pendente, ou aceite até à hora de partida (P-03).
        if (!participacao.PodeSerCanceladaPeloPassageiro(viagem, agora))
            return EstadoNaoPermite;

        if (participacao.EstaAceite)
        {
            var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
                viagem.Id,
                EstadoParticipacao.Aceite,
                ct);

            if (aceites.Count == 0 || !viagem.LugaresConsistentes(aceites.Count))
                return LugaresInconsistentes;

            // RF-F004-17: liberta o lugar.
            viagem.LibertarLugar();

            // RF-F008-05 / P-19: o cancelamento nunca é recusado; sem o Serviço de Mapas, a rota fica por recalcular.
            // Uma participação sem paragens não está na rota, por isso a rota guardada mantém-se (BE-03).
            if (RotaService.AcrescentaParagens(viagem, participacao))
            {
                bool rotaAtualizada = await rotaService.RecalcularAsync(
                    viagem,
                    aceites.Where(p => p.Id != participacao.Id).ToList(),
                    ct);

                if (!rotaAtualizada)
                    viagem.MarcarRotaPorRecalcular();
            }

            // RF-F006-02 e RF-F006-14 / P-23: são notificados o condutor e os restantes passageiros aceites.
            // P-21: cancelar um pedido pendente não gera notificação.
            notificacaoService.RegistarAlteracaoViagem(
                viagem.Id,
                viagem.CondutorId,
                aceites.Where(p => p.Id != participacao.Id).Select(p => p.PassageiroId),
                utilizadorId,
                "Um passageiro cancelou a sua participação na viagem.");
        }

        participacao.CancelarPeloPassageiro(viagem, agora);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }
}
