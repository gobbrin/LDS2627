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
    IRepositorioUtilizadores utilizadores,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IServicoMapasRotas mapas,
    IRotaService rotas,
    IConsultaViagensService consultas,
    EstimativaCustoService custos,
    INotificacaoService notificacoes) : IViagemService
{
    private const string MapasIndisponiveis = "Serviço de rotas indisponível.";
    private const string TempoExcedido = "Tempo de resposta dos mapas excedido.";

    private static readonly Erro DadosInvalidos = new(TipoErro.DadosInvalidos, "Dados de viagem inválidos.");
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
        if (!await utilizadores.ContaConfirmadaAsync(condutorId, ct))
            return new Erro(TipoErro.Proibido, "Confirma a tua conta antes de criar uma viagem.");

        // RF-F002-04: o texto compara-se aqui; as coordenadas, depois de geocodificar.
        if (DadosDaViagemInvalidos(origem, destino, partidaEm, lugares))
            return DadosInvalidos;

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var calculada = await ChamadaMapas.ExecutarAsync<ResultadoRota>(
            async () =>
            {
                var pontoOrigem = await mapas.GeocodificarAsync(origem, ct);
                var pontoDestino = await mapas.GeocodificarAsync(destino, ct);

                if (pontoOrigem is null || pontoDestino is null)
                    return LocalNaoEncontrado;

                if (MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return MesmoLocal;

                var rota = await mapas.CalcularAsync(pontoOrigem, pontoDestino, ct);

                return rota is null || rota.DistanciaKm <= 0 ? RotaNaoCalculada : rota;
            },
            MapasIndisponiveis,
            TempoExcedido,
            ct);

        if (calculada.Erro is { } erro)
            return erro;

        var locais = new LocaisViagem(origem.Trim(), calculada.Valor.Origem, destino.Trim(), calculada.Valor.Destino);
        var viagem = Viagem.Criar(condutorId, locais, partidaEm, lugares, calculada.Valor.DuracaoMinutos);

        RotaGuardada.GuardarDireta(viagem, calculada.Valor);
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
            try
            {
                rota = await rotas.ObterAsync(viagem, aceites, ct);
            }
            catch (HttpRequestException)
            {
                motivo = "Serviço de mapas indisponível.";
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                motivo = "Tempo de resposta do serviço de mapas excedido.";
            }
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
    public Task<IReadOnlyList<ViagemDoCondutor>> ListarDoCondutorAsync(Guid condutorId, CancellationToken ct) =>
        viagens.ListarAtivasDoCondutorAsync(condutorId, ct);

    /// <inheritdoc />
    public async Task<Resultado<List<PassageiroAceite>>> ListarPassageirosAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;

        var passageiros = await participacoes.ListarPassageirosAceitesAsync(viagemId, ct);

        var aceite = passageiros.Any(p => p.PassageiroId == utilizadorId);

        if (!PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceite))
            return Proibido;

        return passageiros.ToList();
    }

    /// <inheritdoc />
    public async Task<Resultado<List<PedidoPendente>>> ListarPedidosPendentesAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return Proibido;

        var pedidos = await participacoes.ListarPedidosPendentesAsync(viagemId, ct);

        return pedidos.ToList();
    }

    /// <inheritdoc />
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
        if (viagem.CondutorId != utilizadorId) return Proibido;

        var agora = DateTime.UtcNow;

        if (!viagem.PorPartir(agora))
            return new Erro(TipoErro.Conflito, "Viagem não editável.");

        var novaOrigem = origem?.Trim() ?? viagem.Origem;
        var novoDestino = destino?.Trim() ?? viagem.Destino;

        // RF-F002-16: as mesmas validações da criação.
        if (DadosDaViagemInvalidos(novaOrigem, novoDestino, partidaEm, lugares))
            return DadosInvalidos;

        if (lugares < viagem.LugaresOcupados)
            return new Erro(TipoErro.Conflito, "Lugares inferiores às participações aceites.");

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        LocaisViagem? locais = null;

        var calculada = await ChamadaMapas.ExecutarAsync<ResultadoRotaComPontos>(
            async () =>
            {
                // Só os locais que mudam são geocodificados de novo.
                var pontoOrigem = novaOrigem == viagem.Origem
                    ? new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude)
                    : await mapas.GeocodificarAsync(novaOrigem, ct);
                var pontoDestino = novoDestino == viagem.Destino
                    ? new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude)
                    : await mapas.GeocodificarAsync(novoDestino, ct);

                if (pontoOrigem is null || pontoDestino is null)
                    return LocalNaoEncontrado;

                if (MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return MesmoLocal;

                locais = new LocaisViagem(novaOrigem, pontoOrigem, novoDestino, pontoDestino);

                // A viagem só muda depois de a rota estar calculada: uma falha aqui deixa-a como estava (RF-F002-17).
                var rota = await rotas.CalcularEntreAsync(
                    new PontoRota(null, TiposPontoRota.Origem, novaOrigem, pontoOrigem),
                    new PontoRota(null, TiposPontoRota.DestinoViagem, novoDestino, pontoDestino),
                    aceites,
                    ct);

                return rota is null ? RotaNaoCalculada : rota;
            },
            MapasIndisponiveis,
            TempoExcedido,
            ct);

        if (calculada.Erro is { } erro)
            return erro;

        viagem.Alterar(locais!, partidaEm, lugares, calculada.Valor.DuracaoTotalMinutos, agora);
        RotaGuardada.Guardar(viagem, calculada.Valor);

        // Notifica os passageiros com participação aceite na viagem alterada.
        notificacoes.RegistarAlteracaoViagem(
            viagem.CondutorId,
            aceites.Select(p => p.PassageiroId),
            utilizadorId,
            "Os dados da viagem em que participas foram alterados.");

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

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

        // RF-F008-05 / P-19: como no cancelamento, a rota sem passageiros nunca impede a operação.
        if (!await rotas.RecalcularAsync(viagem, [], ct))
            viagem.MarcarRotaPorRecalcular();

        // P-23 / RF-F006-03 (DEF-BE-020): notifica os passageiros com pedido pendente ou
        // participação aceite antes da indisponibilização; o condutor originou a alteração.
        notificacoes.RegistarAlteracaoViagem(
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

        viagem.Concluir(agora);

        foreach (var pendente in await participacoes.ListarPendentesParaAlterarAsync(viagemId, ct))
            pendente.CancelarPorConclusao();

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    private static bool DadosDaViagemInvalidos(string origem, string destino, DateTime partidaEm, int lugares) =>
        string.IsNullOrWhiteSpace(origem)
        || string.IsNullOrWhiteSpace(destino)
        || origem.Length > 200
        || destino.Length > 200
        || RegraMesmoLocal.MesmoTexto(origem, destino)
        || partidaEm.Kind != DateTimeKind.Utc
        || partidaEm <= DateTime.UtcNow
        || lugares is < 1 or > 8;

    private static bool MesmasCoordenadas(Coordenadas a, Coordenadas b) =>
        RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
}
