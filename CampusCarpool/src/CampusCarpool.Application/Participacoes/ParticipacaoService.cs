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
    IServicoMapasRotas mapas,
    IRepositorioUtilizadores repositorioUtilizadores) : IParticipacaoService
{
    private const string MensagemPedidoRepetido = "Já existe um pedido ou participação ativa nesta viagem.";

    private const string MensagemPontosIncompativeis =
        "A recolha e o destino não podem ser o mesmo local, a recolha não pode ser o destino da viagem e o destino não pode ser a origem da viagem.";

    private static readonly Erro PontosIncompativeisErro = new(TipoErro.DadosInvalidos, MensagemPontosIncompativeis);
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

    // RF-F004-18: recolha e destino efetivos; RF-F004-20: recolha no destino da viagem; RF-F004-21: destino na origem.
    private static bool PontosIncompativeis<T>(
        T recolha,
        T destino,
        T origemViagem,
        T destinoViagem,
        Func<T, T, bool> mesmoLocal) =>
        mesmoLocal(recolha, destino)
        || mesmoLocal(recolha, destinoViagem)
        || mesmoLocal(destino, origemViagem);

    /// <inheritdoc />
    public async Task<Resultado<PedidoCriado>> CriarPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        // Confirmar que o passageiro tem uma conta validada.
        if (!await repositorioUtilizadores.ContaConfirmadaAsync(passageiroId, ct))
            return new Erro(TipoErro.Proibido, "Confirma a tua conta antes de pedir participação numa viagem.");

        // A viagem deve existir, estar ativa e ainda não ter começado.
        var viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        if (!viagem.PorPartir(DateTime.UtcNow))
            return new Erro(TipoErro.Conflito, "Viagem indisponível.");

        if (viagem.CondutorId == passageiroId)
            return new Erro(TipoErro.Conflito, "Não podes pedir participação na tua própria viagem.");

        // Impedir pedidos pendentes ou participações aceites duplicadas.
        if (await repositorioParticipacoes.ExisteAtivaAsync(viagem.Id, passageiroId, ct))
            return new Erro(TipoErro.Conflito, MensagemPedidoRepetido);

        // Apenas as participações aceites ocupam lugares.
        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(viagem.Id, EstadoParticipacao.Aceite, ct);

        if (aceites.Count >= viagem.LugaresOferecidos)
            return new Erro(TipoErro.Conflito, "Não existem lugares disponíveis nesta viagem.");

        // P-20: pelo menos um dos dois pontos; os valores fornecidos devem ser válidos.
        if (recolha is null && destino is null)
            return new Erro(TipoErro.DadosInvalidos, "Indica um ponto de recolha ou de destino.");

        if ((recolha is not null && (string.IsNullOrWhiteSpace(recolha) || recolha.Length > 200))
            || (destino is not null && (string.IsNullOrWhiteSpace(destino) || destino.Length > 200)))
        {
            return new Erro(TipoErro.DadosInvalidos, "Os pontos de recolha ou destino são inválidos.");
        }

        // Primeiro só pelo texto, para não chamar o serviço de mapas sem necessidade.
        if (PontosIncompativeis(
            recolha ?? viagem.Origem,
            destino ?? viagem.Destino,
            viagem.Origem,
            viagem.Destino,
            RegraMesmoLocal.MesmoTexto))
        {
            return PontosIncompativeisErro;
        }

        // Verificar se os locais fornecidos podem ser identificados pelo serviço de mapas.
        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var pontos = await ChamadaMapas.ExecutarAsync<PontosDoPedido>(
            async () =>
            {
                Coordenadas? coordenadasRecolha = null;
                Coordenadas? coordenadasDestino = null;

                if (recolha is not null)
                {
                    coordenadasRecolha = await mapas.GeocodificarAsync(recolha.Trim(), ct);

                    if (coordenadasRecolha is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de recolha não encontrado.");
                }

                if (destino is not null)
                {
                    coordenadasDestino = await mapas.GeocodificarAsync(destino.Trim(), ct);

                    if (coordenadasDestino is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de destino não encontrado.");
                }

                return new PontosDoPedido(coordenadasRecolha, coordenadasDestino);
            },
            "Serviço de mapas indisponível.",
            "Tempo de resposta dos mapas excedido.",
            ct);

        if (pontos.Erro is { } erroMapas)
            return erroMapas;

        // As mesmas regras, agora pelas coordenadas. Um ponto omitido usa as coordenadas da viagem (P-20).
        var origemViagem = new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude);
        var destinoViagem = new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude);

        if (PontosIncompativeis(
            pontos.Valor.Recolha ?? origemViagem,
            pontos.Valor.Destino ?? destinoViagem,
            origemViagem,
            destinoViagem,
            (a, b) => RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude)))
        {
            return PontosIncompativeisErro;
        }

        // Criar a participação pendente sem descontar lugares.
        var pedido = new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiroId,
            Recolha = recolha?.Trim(),
            Destino = destino?.Trim(),
            RecolhaLatitude = pontos.Valor.Recolha?.Latitude,
            RecolhaLongitude = pontos.Valor.Recolha?.Longitude,
            DestinoLatitude = pontos.Valor.Destino?.Latitude,
            DestinoLongitude = pontos.Valor.Destino?.Longitude,
            CriadoEm = DateTime.UtcNow
        };

        var notificacao = notificacaoService.Registar(
            viagem.CondutorId,
            TipoNotificacao.PedidoRecebido,
            "Recebeste um novo pedido de participação numa viagem.");

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

        if (viagem.CondutorId != utilizadorId)
            return new Erro(TipoErro.Proibido, "Só o condutor da viagem pode simular este pedido.");

        // A simulação antecede a decisão (RF-F004-09), por isso segue a mesma regra da aceitação (RF-F004-10).
        if (!viagem.EstaAtiva)
            return new Erro(TipoErro.Conflito, "A viagem não está ativa.");

        var pedido = await repositorioParticipacoes.ObterPorIdAsync(pedidoId, ct);

        if (pedido is null || pedido.ViagemId != viagemId)
            return new Erro(TipoErro.NaoEncontrado, "Pedido não encontrado nesta viagem.");

        if (!pedido.EstaPendente)
            return new Erro(TipoErro.Conflito, "Só é possível simular pedidos pendentes.");

        if (string.IsNullOrWhiteSpace(pedido.Recolha) && string.IsNullOrWhiteSpace(pedido.Destino))
            return new Erro(TipoErro.DadosInvalidos, "O pedido deve indicar recolha ou destino.");

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        var recolha = string.IsNullOrWhiteSpace(pedido.Recolha) ? viagem.Origem : pedido.Recolha.Trim();
        var destino = string.IsNullOrWhiteSpace(pedido.Destino) ? viagem.Destino : pedido.Destino.Trim();

        if (RegraMesmoLocal.MesmoTexto(recolha, destino))
            return new Erro(TipoErro.DadosInvalidos, "A recolha e o destino efetivos não podem ser iguais.");

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(10));

        const string MensagemTempo = "O serviço de mapas excedeu o limite de 10 segundos.";

        return await ChamadaMapas.ExecutarAsync<SimulacaoPedido>(
            async () =>
            {
                try
                {
                    // A rota atual é a guardada; só falta calculá-la se estiver por recalcular (P-19). Nada é gravado.
                    var original = RotaGuardada.Ler(viagem) ?? await rotaService.CalcularAsync(viagem, aceites, limite.Token);
                    var simulada = await rotaService.CalcularAsync(viagem, [.. aceites, pedido], limite.Token);

                    if (original is null || simulada is null)
                        return new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

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
                catch (ArgumentException ex)
                {
                    return new Erro(TipoErro.DadosInvalidos, ex.Message);
                }
            },
            "Falha na comunicação com o serviço de mapas.",
            MensagemTempo,
            ct);
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

        if (viagem.LugaresDisponiveis == 0)
            return SemCapacidade;

        // RF-F008-05 / P-19: a aceitação recalcula a rota; sem o Serviço de Mapas é recusada e nada muda.
        var rotaAtualizada = await rotaService.RecalcularAsync(
            viagem,
            [.. aceites, participacao],
            ct);

        if (!rotaAtualizada)
            return RotaIndisponivel;

        participacao.Aceitar(DateTime.UtcNow);
        viagem.OcuparLugar();

        var notificacao = notificacaoService.Registar(
            participacao.PassageiroId,
            TipoNotificacao.PedidoAceite,
            "O teu pedido de participação na viagem foi aceite.");

        // RF-F006-14 / P-23: o novo passageiro recebe Pedido aceite e não Alteração da viagem pela sua entrada.
        notificacaoService.RegistarAlteracaoViagem(
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
            "O teu pedido de participação na viagem foi rejeitado.");

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
            var rotaAtualizada = await rotaService.RecalcularAsync(
                viagem,
                aceites.Where(p => p.Id != participacao.Id).ToList(),
                ct);

            if (!rotaAtualizada)
                viagem.MarcarRotaPorRecalcular();

            // RF-F006-02 e RF-F006-14 / P-23: são notificados o condutor e os restantes passageiros aceites.
            // P-21: cancelar um pedido pendente não gera notificação.
            notificacaoService.RegistarAlteracaoViagem(
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
