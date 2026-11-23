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
/// <remarks>
/// As transições de estado estão nas entidades (AD-001); aqui coordenam-se os dados, a rota e as notificações. Na fase
/// R saíram daqui a simulação da alteração (<see cref="SimulacaoDeAlteracaoService"/>, AE-01) e o fim do ciclo de vida
/// (<see cref="CicloDeVidaDaViagemService"/>, AE-02).
/// </remarks>
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
    // As regras e o cálculo da rota de uma alteração de origem ou destino, partilhados com a simulação (AE-01).
    private readonly AlteracaoDeViagem alteracao = new(mapas, rotas);

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
                    return ErrosDaViagem.LocalNaoEncontrado;

                if (AlteracaoDeViagem.MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return ErrosDaViagem.MesmoLocal;

                var rota = await mapas.CalcularAsync(pontoOrigem, pontoDestino, limite);

                return rota is null || rota.DistanciaKm <= 0 ? ErrosDaViagem.RotaNaoCalculada : rota;
            },
            ErrosDaViagem.MapasIndisponiveis,
            ErrosDaViagem.TempoExcedido,
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
            return ErrosDaViagem.NaoEncontrada;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        // A rota guardada lê-se sem chamadas. Uma rota por recalcular calcula-se aqui, com a linha fora do limite de
        // 10 s (BE-36), e só numa viagem ativa (BE-38 d). Uma falha dos mapas não impede a consulta, só a estimativa.
        Resultado<ResultadoRotaComPontos> obtida = await rotas.ObterAsync(viagem, aceites, ct);
        ResultadoRotaComPontos? rota = obtida.Erro is null ? obtida.Valor : null;
        string? motivo = obtida.Erro?.Mensagem;
        ResultadoEstimativaCusto? estimativa = null;

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
    public async Task<PaginaViagensDoCondutor> ListarDoCondutorAsync(Guid condutorId, int pagina, int tamanho, CancellationToken ct)
    {
        PaginaDe<Viagem> ativas = await viagens.ListarAtivasDoCondutorAsync(condutorId, pagina, tamanho, ct);
        var resposta = new List<ViagemDoCondutor>();

        // O repositório devolve as entidades; a resposta da API monta-se aqui (BE-07).
        foreach (Viagem viagem in ativas.Itens)
        {
            resposta.Add(new ViagemDoCondutor(
                viagem.Id,
                viagem.Origem,
                viagem.Destino,
                viagem.PartidaEm,
                viagem.LugaresOferecidos,
                viagem.LugaresDisponiveis));
        }

        return new PaginaViagensDoCondutor(ativas.Total, pagina, tamanho, resposta);
    }

    /// <inheritdoc />
    public async Task<Resultado<List<PassageiroAceite>>> ListarPassageirosAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;

        IReadOnlyList<Participacao> aceites = await participacoes.ListarPassageirosAceitesAsync(viagemId, ct);

        var aceite = aceites.Any(p => p.PassageiroId == utilizadorId);

        if (!PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceite))
            return ErrosDaViagem.ProibidoConsultar;

        // BE-37 (P-13): todos os aceites veem os pontos dos outros (D-1), mas só o condutor recebe o id da participação.
        bool eCondutor = viagem.CondutorId == utilizadorId;

        // O repositório devolve as entidades; a resposta da API monta-se aqui (BE-07).
        var passageiros = new List<PassageiroAceite>();

        foreach (Participacao participacao in aceites)
        {
            passageiros.Add(new PassageiroAceite(
                eCondutor ? participacao.Id : null,
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
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return ErrosDaViagem.Proibido;

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
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;

        var agora = DateTime.UtcNow;

        if (AlteracaoDeViagem.ValidarQuemAltera(viagem, utilizadorId, agora) is { } recusado)
            return recusado;

        // Um local omitido mantém-se.
        string novaOrigem = origem?.Trim() ?? viagem.Origem;
        string novoDestino = destino?.Trim() ?? viagem.Destino;

        // RF-F002-16: as mesmas validações da criação.
        if (ValidarDadosDaViagem(novaOrigem, novoDestino, partidaEm, lugares) is { } invalido)
            return invalido;

        if (lugares < viagem.LugaresOcupados)
            return new Erro(TipoErro.Conflito, "Os lugares não podem ser inferiores às participações aceites.");

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        // BE-38 c: «Porto» e «porto» são o mesmo local (RegraMesmoLocal), por isso não chamam os mapas.
        if (RegraMesmoLocal.MesmoTexto(novaOrigem, viagem.Origem) && RegraMesmoLocal.MesmoTexto(novoDestino, viagem.Destino))
        {
            // Os locais não mudam: a rota, as coordenadas e a duração ficam como estão; só o texto pode mudar.
            viagem.Alterar(LocaisComNovoTexto(viagem, novaOrigem, novoDestino), partidaEm, lugares, viagem.DuracaoMinutos, agora);
        }
        else
        {
            // A viagem só muda depois de a rota estar calculada: uma falha aqui deixa-a como estava (RF-F002-17).
            Resultado<AlteracaoDeViagem.NovaRota> calculada = await alteracao.CalcularRotaComNovosLocaisAsync(viagem, novaOrigem, novoDestino, aceites, ct);

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

    /// <summary>
    /// Locais de uma edição que não muda de sítio: o texto indicado (pode mudar só a capitalização ou os espaços,
    /// BE-38 c) com as coordenadas já guardadas.
    /// </summary>
    /// <remarks>Os pontos da rota guardada mantêm o texto antigo até ao próximo recálculo da rota.</remarks>
    private static LocaisViagem LocaisComNovoTexto(Viagem viagem, string novaOrigem, string novoDestino) => new(
        novaOrigem,
        new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude),
        novoDestino,
        new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude));

    /// <summary>
    /// Valida os dados de uma viagem antes de chamar os mapas (criação e edição, RF-F002-16).
    /// </summary>
    /// <returns>O erro da primeira regra que falha, com uma mensagem própria (BE-15); <c>null</c> se estiver tudo certo.</returns>
    private static Erro? ValidarDadosDaViagem(string origem, string destino, DateTime partidaEm, int lugares)
    {
        if (AlteracaoDeViagem.ValidarLocais(origem, destino) is { } locaisInvalidos)
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
}
