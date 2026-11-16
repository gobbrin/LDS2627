using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Viagens;

/// <summary>Implementação de <see cref="IViagemService"/>.</summary>
/// <remarks>As transições de estado estão nas entidades (AD-001); aqui coordenam-se os dados, a rota e as notificações.</remarks>
public sealed class ViagemService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IServicoMapasRotas mapas,
    IRotaService rotas,
    IConsultaViagensService consultas,
    EstimativaCustoService custos,
    INotificacaoService notificacoes) : IViagemService
{
    private const string MapasIndisponiveis = "Serviço de mapas indisponível.";
    private const string TempoExcedido = "Tempo de resposta do serviço de mapas excedido.";

    // Tamanho máximo dos textos de origem e destino (coluna da BD).
    private const int TamanhoMaximoLocal = 200;

    private static readonly Erro NaoEncontrada = new(TipoErro.NaoEncontrado, "Viagem não encontrada.");
    private static readonly Erro Proibido = new(TipoErro.Proibido, "Não tens permissão para gerir esta viagem.");
    private static readonly Erro LocalNaoEncontrado = new(TipoErro.NaoProcessavel, "Origem ou destino não encontrado.");
    private static readonly Erro RotaNaoCalculada = new(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");
    private static readonly Erro MesmoLocal = new(TipoErro.DadosInvalidos, "A origem e o destino não podem ser o mesmo local.");

    /// <inheritdoc />
    public async Task<Resultado<ViagemCriada>> CriarAsync(
        Guid condutorId,
        string origem,
        string destino,
        DateTime partidaEm,
        int lugares,
        CancellationToken ct)
    {
        // RF-F002-04: o texto compara-se aqui; as coordenadas, depois de geocodificar.
        if (ValidarDadosDaViagem(origem, destino, partidaEm, lugares) is { } invalido)
            return invalido;

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var calculada = await ChamadaMapas.ExecutarAsync<ResultadoRota>(
            async limite =>
            {
                var pontoOrigem = await mapas.GeocodificarAsync(origem, limite);
                var pontoDestino = await mapas.GeocodificarAsync(destino, limite);

                if (pontoOrigem is null || pontoDestino is null)
                    return LocalNaoEncontrado;

                if (MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return MesmoLocal;

                var rota = await mapas.CalcularAsync(pontoOrigem, pontoDestino, limite);

                return rota is null || rota.DistanciaKm <= 0 ? RotaNaoCalculada : rota;
            },
            MapasIndisponiveis,
            TempoExcedido,
            ct);

        if (calculada.Erro is { } erro)
            return erro;

        var locais = new LocaisViagem(origem.Trim(), calculada.Valor.Origem, destino.Trim(), calculada.Valor.Destino);
        var viagem = Viagem.Criar(condutorId, locais, partidaEm, lugares, calculada.Valor.DuracaoMinutos);

        // A linha do mapa (HERE) junta-se aqui; sem ela, a viagem é criada na mesma (DEF-BE-054).
        await rotas.GuardarAsync(viagem, RotaGuardada.Direta(viagem, calculada.Valor), ct);
        viagens.Adicionar(viagem);
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return new ViagemCriada(
            viagem.Id,
            viagem.Origem,
            viagem.Destino,
            viagem.PartidaEm,
            viagem.LugaresDisponiveis,
            viagem.DistanciaKm,
            viagem.DuracaoMinutos);
    }

    /// <inheritdoc />
    public async Task<Resultado<DetalhesViagem>> ConsultarAsync(Guid viagemId, Guid? utilizadorId, CancellationToken ct)
    {
        // Seguida pelo contexto: uma rota por recalcular é gravada ao ser obtida.
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null || !await consultas.PodeConsultarAsync(viagem, utilizadorId, ct))
            return NaoEncontrada;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        var rota = RotaGuardada.Ler(viagem);
        ResultadoEstimativaCusto? estimativa = null;
        string? motivo = null;

        if (rota is null && !mapas.Configurado)
        {
            motivo = "Serviço de mapas não configurado.";
        }
        else
        {
            // Uma rota por recalcular calcula-se aqui; uma falha dos mapas não impede a consulta, só a estimativa.
            Resultado<ResultadoRotaComPontos?> obtida = await ChamadaMapas.ExecutarAsync<ResultadoRotaComPontos?>(
                async limite => await rotas.ObterAsync(viagem, aceites, limite),
                MapasIndisponiveis,
                TempoExcedido,
                ct);

            rota = obtida.Erro is null ? obtida.Valor : null;
            motivo = obtida.Erro?.Mensagem;
        }

        if (rota is { DistanciaTotalKm: > 0 })
        {
            // Erros de configuração não são silenciosamente convertidos em valores indicativos.
            estimativa = custos.Calcular(rota.DistanciaTotalKm, aceites.Count);
        }
        else
        {
            motivo ??= "Dados de distância insuficientes para calcular a estimativa.";
        }

        var condutores = await consultas.CondutoresAsync([viagem.CondutorId], ct);

        return new DetalhesViagem(
            viagem.Id,
            viagem.Origem,
            viagem.Destino,
            viagem.PartidaEm,
            viagem.LugaresDisponiveis,
            viagem.Estado.ToString(),
            rota?.DistanciaTotalKm ?? viagem.DistanciaKm,
            rota?.DuracaoTotalMinutos ?? viagem.DuracaoMinutos,
            viagem.CondutorId,
            condutores.GetValueOrDefault(viagem.CondutorId),
            RotaGuardada.ContarAdicionais(viagem.RotaPontos),
            estimativa,
            motivo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemDoCondutor>> ListarDoCondutorAsync(Guid condutorId, CancellationToken ct)
    {
        IReadOnlyList<Viagem> ativas = await viagens.ListarAtivasDoCondutorAsync(condutorId, ct);
        var resposta = new List<ViagemDoCondutor>();

        // O repositório devolve as entidades; a resposta da API monta-se aqui (BE-07).
        foreach (Viagem viagem in ativas)
        {
            resposta.Add(new ViagemDoCondutor(
                viagem.Id,
                viagem.Origem,
                viagem.Destino,
                viagem.PartidaEm,
                viagem.LugaresOferecidos,
                viagem.LugaresDisponiveis));
        }

        return resposta;
    }

    /// <inheritdoc />
    public async Task<Resultado<List<PassageiroAceite>>> ListarPassageirosAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;

        IReadOnlyList<Participacao> aceites = await participacoes.ListarPassageirosAceitesAsync(viagemId, ct);

        var aceite = aceites.Any(p => p.PassageiroId == utilizadorId);

        if (!PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceite))
            return Proibido;

        // O repositório devolve as entidades; a resposta da API monta-se aqui (BE-07).
        var passageiros = new List<PassageiroAceite>();

        foreach (Participacao participacao in aceites)
        {
            passageiros.Add(new PassageiroAceite(
                participacao.Id,
                participacao.PassageiroId,
                participacao.Passageiro.Nome,
                participacao.Recolha,
                participacao.Destino));
        }

        return passageiros;
    }

    /// <inheritdoc />
    public async Task<Resultado<List<PedidoPendente>>> ListarPedidosPendentesAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return Proibido;

        IReadOnlyList<Participacao> pendentes = await participacoes.ListarPedidosPendentesAsync(viagemId, ct);

        // O repositório devolve as entidades; a resposta da API monta-se aqui (BE-07).
        var pedidos = new List<PedidoPendente>();

        foreach (Participacao pedido in pendentes)
        {
            pedidos.Add(new PedidoPendente(
                pedido.Id,
                pedido.PassageiroId,
                pedido.Passageiro.Nome,
                pedido.Recolha,
                pedido.Destino,
                pedido.CriadoEm));
        }

        return pedidos;
    }

    /// <inheritdoc />
    /// <remarks>
    /// RF-F002-17 (decisão de 16/11, BE-04): o Serviço de Mapas só é preciso quando a origem ou o destino mudam, porque
    /// só aí há rota a recalcular. Mudar só a hora ou os lugares mantém a rota e a duração guardadas, sem chamar os mapas.
    /// </remarks>
    public async Task<Resultado> EditarAsync(
        Guid viagemId,
        Guid utilizadorId,
        DateTime partidaEm,
        int lugares,
        string? origem,
        string? destino,
        CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;

        var agora = DateTime.UtcNow;

        if (ValidarQuemAltera(viagem, utilizadorId, agora) is { } recusado)
            return recusado;

        // Um local omitido mantém-se.
        string novaOrigem = origem?.Trim() ?? viagem.Origem;
        string novoDestino = destino?.Trim() ?? viagem.Destino;

        // RF-F002-16: as mesmas validações da criação.
        if (ValidarDadosDaViagem(novaOrigem, novoDestino, partidaEm, lugares) is { } invalido)
            return invalido;

        if (lugares < viagem.LugaresOcupados)
            return new Erro(TipoErro.Conflito, "Lugares inferiores às participações aceites.");

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        if (novaOrigem == viagem.Origem && novoDestino == viagem.Destino)
        {
            // Os locais não mudam: a rota e a duração ficam como estão.
            viagem.Alterar(LocaisAtuais(viagem), partidaEm, lugares, viagem.DuracaoMinutos, agora);
        }
        else
        {
            // A viagem só muda depois de a rota estar calculada: uma falha aqui deixa-a como estava (RF-F002-17).
            Resultado<NovaRota> calculada = await CalcularRotaComNovosLocaisAsync(viagem, novaOrigem, novoDestino, aceites, ct);

            if (calculada.Erro is { } erro)
                return erro;

            viagem.Alterar(calculada.Valor.Locais, partidaEm, lugares, calculada.Valor.Rota.DuracaoTotalMinutos, agora);

            // GuardarAsync compara com a rota ainda guardada: se os pontos forem os mesmos, reutiliza a linha do mapa.
            await rotas.GuardarAsync(viagem, calculada.Valor.Rota, ct);
        }

        // Notifica os passageiros com participação aceite na viagem alterada.
        notificacoes.RegistarAlteracaoViagem(
            viagem.Id,
            viagem.CondutorId,
            aceites.Select(p => p.PassageiroId),
            utilizadorId,
            "Os dados da viagem em que participas foram alterados.");

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Faz as mesmas verificações e o mesmo cálculo da alteração (<see cref="EditarAsync"/>), mas não grava nada, não
    /// notifica ninguém e não pede a linha à HERE: a linha só se pede ao gravar (DEF-BE-054). Por isso o mapa da
    /// simulação mostra retas entre os pontos (<c>GeometriaAproximada = true</c>).
    /// </remarks>
    public async Task<Resultado<RotaDaViagem>> SimularAlteracaoAsync(
        Guid viagemId,
        Guid utilizadorId,
        string? origem,
        string? destino,
        CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;

        if (ValidarQuemAltera(viagem, utilizadorId, DateTime.UtcNow) is { } recusado)
            return recusado;

        // Como no PUT: um local omitido mantém-se.
        string novaOrigem = origem?.Trim() ?? viagem.Origem;
        string novoDestino = destino?.Trim() ?? viagem.Destino;

        if (ValidarLocais(novaOrigem, novoDestino) is { } invalido)
            return invalido;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        // R-B01: com os mesmos locais, a rota simulada é a guardada; os mapas só se chamam se ela estiver por recalcular.
        ResultadoRotaComPontos? rota = RotaGuardada.Ler(viagem);
        bool locaisMudam = novaOrigem != viagem.Origem || novoDestino != viagem.Destino;

        if (rota is null || locaisMudam)
        {
            // O mesmo cálculo do PUT, com as paragens dos passageiros aceites (RF-F002-07, RF-F008-05).
            Resultado<NovaRota> calculada = await CalcularRotaComNovosLocaisAsync(viagem, novaOrigem, novoDestino, aceites, ct);

            if (calculada.Erro is { } erro)
                return erro;

            rota = calculada.Valor.Rota;
        }

        // O custo exige uma distância positiva (EstimativaCustoService).
        if (rota.DistanciaTotalKm <= 0)
            return RotaNaoCalculada;

        return RespostaDaSimulacao(viagem.Id, aceites.Count, rota);
    }

    /// <summary>Converte a rota simulada na resposta da API, com o custo para os passageiros aceites (RF-F008-10).</summary>
    /// <param name="viagemId">Viagem simulada.</param>
    /// <param name="passageirosAceites">Número de passageiros aceites (entram no custo por participante).</param>
    /// <param name="rota">Rota simulada (ou a guardada, se os locais não mudam).</param>
    /// <returns>A rota no mesmo formato de <c>GET /rotas/viagens/{id}</c>: o condutor vê todos os pontos.</returns>
    private RotaDaViagem RespostaDaSimulacao(Guid viagemId, int passageirosAceites, ResultadoRotaComPontos rota)
    {
        // Sem linha da HERE (rota calculada agora), o mapa usa retas entre os pontos pela ordem da rota.
        IReadOnlyList<Coordenadas> linha = rota.Geometria ?? GeometriaRota.LinhaReta(rota.PontosOrdenados);

        return new RotaDaViagem(
            viagemId,
            passageirosAceites,
            ConsultaRotasService.PontosParaResposta(rota),
            ConsultaRotasService.TrocosParaResposta(rota),
            rota.DistanciaTotalKm,
            rota.DuracaoTotalMinutos,
            custos.Calcular(rota.DistanciaTotalKm, passageirosAceites),
            GeometriaRota.EmPares(linha),
            rota.Geometria is null);
    }

    /// <summary>
    /// Regras de quem pode alterar (ou simular a alteração de) uma viagem: só o condutor, com a viagem ativa e ainda
    /// por partir (RF-F002-07).
    /// </summary>
    /// <returns>O erro 403 ou 409; <c>null</c> se a alteração for permitida.</returns>
    private static Erro? ValidarQuemAltera(Viagem viagem, Guid utilizadorId, DateTime agora)
    {
        if (viagem.CondutorId != utilizadorId)
            return Proibido;

        if (!viagem.PorPartir(agora))
            return new Erro(TipoErro.Conflito, "Viagem não editável.");

        return null;
    }

    // Locais e rota calculados para uma edição que muda a origem ou o destino.
    private sealed record NovaRota(LocaisViagem Locais, ResultadoRotaComPontos Rota);

    /// <summary>
    /// Obtém as coordenadas dos locais novos e calcula a rota com os passageiros aceites (RF-F002-07, RF-F008-05).
    /// Não altera a viagem.
    /// </summary>
    /// <returns>Os locais e a rota; ou o erro: 400 mesmo local, 422 local ou rota não encontrados, 502/503/504 mapas.</returns>
    private async Task<Resultado<NovaRota>> CalcularRotaComNovosLocaisAsync(
        Viagem viagem,
        string novaOrigem,
        string novoDestino,
        IReadOnlyList<Participacao> aceites,
        CancellationToken ct)
    {
        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<NovaRota>(
            async limite =>
            {
                // Só o local que muda é geocodificado de novo.
                var pontoOrigem = novaOrigem == viagem.Origem
                    ? new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude)
                    : await mapas.GeocodificarAsync(novaOrigem, limite);
                var pontoDestino = novoDestino == viagem.Destino
                    ? new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude)
                    : await mapas.GeocodificarAsync(novoDestino, limite);

                if (pontoOrigem is null || pontoDestino is null)
                    return LocalNaoEncontrado;

                if (MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return MesmoLocal;

                var rota = await rotas.CalcularEntreAsync(
                    new PontoRota(null, TiposPontoRota.Origem, novaOrigem, pontoOrigem),
                    new PontoRota(null, TiposPontoRota.DestinoViagem, novoDestino, pontoDestino),
                    aceites,
                    limite);

                if (rota is null)
                    return RotaNaoCalculada;

                return new NovaRota(new LocaisViagem(novaOrigem, pontoOrigem, novoDestino, pontoDestino), rota);
            },
            MapasIndisponiveis,
            TempoExcedido,
            ct);
    }

    // Origem e destino atuais da viagem, com as coordenadas guardadas.
    private static LocaisViagem LocaisAtuais(Viagem viagem) => new(
        viagem.Origem,
        new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude),
        viagem.Destino,
        new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude));

    /// <inheritdoc />
    public async Task<Resultado> IndisponibilizarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return Proibido;
        if (!viagem.EstaAtiva) return new Erro(TipoErro.Conflito, "A viagem já não está ativa.");

        viagem.Indisponibilizar();

        // P-25 / RF-F002-10 (DEF-BE-008): cancela os pedidos pendentes e as participações aceites.
        var afetadas = await participacoes.ListarAtivasParaAlterarAsync(viagemId, ct);

        foreach (var participacao in afetadas)
            participacao.CancelarPorIndisponibilidade();

        // BE-29: a indisponibilização não chama os mapas (P-19). A viagem já não se realiza, por isso não se gasta um
        // pedido para recalcular a rota sem passageiros: sem paragens, a rota guardada continua certa; com paragens,
        // fica por recalcular e só se calcula se alguém a consultar.
        ResultadoRotaComPontos? guardada = RotaGuardada.Ler(viagem);

        if (guardada is not null && guardada.PontosOrdenados.Count > 2)
            viagem.MarcarRotaPorRecalcular();

        // P-23 / RF-F006-03 (DEF-BE-020): notifica os passageiros com pedido pendente ou
        // participação aceite antes da indisponibilização; o condutor originou a alteração.
        notificacoes.RegistarAlteracaoViagem(
            viagem.Id,
            viagem.CondutorId,
            afetadas.Select(p => p.PassageiroId),
            utilizadorId,
            "A viagem foi marcada como indisponível pelo condutor e o teu pedido ou participação foi cancelado.");

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> ConcluirAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return Proibido;

        var agora = DateTime.UtcNow;

        if (!viagem.PodeConcluir(agora))
            return new Erro(TipoErro.Conflito, "A viagem só pode ser concluída depois da partida e enquanto estiver ativa.");

        // RF-F004-19: os pedidos pendentes são cancelados com a conclusão (regra na entidade, BE-31).
        var pendentes = await participacoes.ListarPendentesParaAlterarAsync(viagemId, ct);
        viagem.Concluir(agora, pendentes);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <summary>
    /// Valida os dados de uma viagem antes de chamar os mapas (criação e edição, RF-F002-16).
    /// </summary>
    /// <returns>O erro da primeira regra que falha, com uma mensagem própria (BE-15); <c>null</c> se estiver tudo certo.</returns>
    private static Erro? ValidarDadosDaViagem(string origem, string destino, DateTime partidaEm, int lugares)
    {
        if (ValidarLocais(origem, destino) is { } locaisInvalidos)
            return locaisInvalidos;

        // A API só aceita datas em UTC (com Z); outro desvio chega aqui com outro Kind.
        if (partidaEm.Kind != DateTimeKind.Utc)
            return new Erro(TipoErro.DadosInvalidos, "A hora de partida tem de vir em UTC (terminada em Z).");

        if (partidaEm <= DateTime.UtcNow)
            return new Erro(TipoErro.DadosInvalidos, "A hora de partida tem de ser no futuro.");

        // P-11: a mesma regra que Viagem.Criar e Viagem.Alterar garantem.
        if (lugares < Viagem.LugaresMinimos || lugares > Viagem.LugaresMaximos)
            return new Erro(TipoErro.DadosInvalidos, "O número de lugares tem de estar entre 1 e 8.");

        return null;
    }

    /// <summary>
    /// Valida a origem e o destino pelo texto: preenchidos, até 200 caracteres e diferentes (criação, alteração e
    /// simulação da alteração, RF-F002-04, RF-F002-16).
    /// </summary>
    /// <returns>O erro da primeira regra que falha; <c>null</c> se estiver tudo certo.</returns>
    private static Erro? ValidarLocais(string origem, string destino)
    {
        if (string.IsNullOrWhiteSpace(origem))
            return new Erro(TipoErro.DadosInvalidos, "Indica a origem da viagem.");

        if (string.IsNullOrWhiteSpace(destino))
            return new Erro(TipoErro.DadosInvalidos, "Indica o destino da viagem.");

        if (origem.Length > TamanhoMaximoLocal)
            return new Erro(TipoErro.DadosInvalidos, "A origem tem no máximo 200 caracteres.");

        if (destino.Length > TamanhoMaximoLocal)
            return new Erro(TipoErro.DadosInvalidos, "O destino tem no máximo 200 caracteres.");

        // RF-F002-04: origem e destino diferentes (aqui pelo texto; pelas coordenadas depois de geocodificar).
        if (RegraMesmoLocal.MesmoTexto(origem, destino))
            return MesmoLocal;

        return null;
    }

    private static bool MesmasCoordenadas(Coordenadas a, Coordenadas b) =>
        RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
}
