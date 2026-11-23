using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Participacoes;

/// <summary>
/// Simulação do percurso de uma viagem com um pedido (RF-F004-09, RF-F008-06; AE-01): a do condutor, com um pedido
/// pendente, e a do passageiro, com um pedido que ainda vai enviar (ecrã 14 do protótipo).
/// </summary>
/// <remarks>
/// <para>Saiu do <c>ParticipacaoService</c> na I112 (AE-01): é o ponto que gasta a API de mapas, e assim fica numa
/// classe pequena, só com ele.</para>
/// <para>Passos comuns: (1) validar a viagem e o pedido, sem mapas; (2) obter a rota atual e a rota com o pedido
/// (<see cref="CalcularRotasAsync"/>); (3) montar a resposta, com a rota simulada no formato de
/// <c>GET /rotas/viagens/{id}</c> (BE-35, D-2). O condutor vê todos os pontos; o passageiro só a origem, o destino e
/// os seus (P-13).</para>
/// </remarks>
public sealed class SimulacaoDePedidoService(
    IRepositorioViagens repositorioViagens,
    IRepositorioParticipacoes repositorioParticipacoes,
    IRotaService rotaService,
    IServicoMapasRotas mapas,
    EstimativaCustoService custos) : ISimulacaoDePedidoService
{
    private const string MensagemTempo = "O serviço de mapas excedeu o limite de 10 segundos.";

    private static readonly Erro RotaNaoCalculada = new(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

    // A rota atual da viagem e a rota se o pedido for aceite.
    private sealed record RotasDaSimulacao(ResultadoRotaComPontos Original, ResultadoRotaComPontos Simulada);

    // As regras do pedido real (POST /pedidos), usadas na simulação do passageiro.
    private readonly PreparacaoDePedido preparacao = new(repositorioParticipacoes, mapas);

    /// <inheritdoc />
    public async Task<Resultado<SimulacaoPedido>> SimularAsync(
        Guid viagemId,
        Guid pedidoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        // 1. Validações, sem chamar os mapas.
        Viagem? viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        if (ValidarViagem(viagem, utilizadorId) is { } erroViagem)
            return erroViagem;

        Participacao? pedido = await repositorioParticipacoes.ObterPorIdAsync(pedidoId, ct);

        if (pedido is null || pedido.ViagemId != viagemId)
            return new Erro(TipoErro.NaoEncontrado, "Pedido não encontrado nesta viagem.");

        if (ValidarPedido(viagem, pedido) is { } erroPedido)
            return erroPedido;

        // Um ponto omitido segue a origem ou o destino da viagem (P-20).
        string recolha = string.IsNullOrWhiteSpace(pedido.Recolha) ? viagem.Origem : pedido.Recolha.Trim();
        string destino = string.IsNullOrWhiteSpace(pedido.Destino) ? viagem.Destino : pedido.Destino.Trim();

        if (RegraMesmoLocal.MesmoTexto(recolha, destino))
            return new Erro(TipoErro.DadosInvalidos, "A recolha e o destino efetivos não podem ser iguais.");

        // 2. Rota atual e rota com o pedido.
        IReadOnlyList<Participacao> aceites = await repositorioParticipacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);
        Resultado<RotasDaSimulacao> rotas = await CalcularRotasAsync(viagem, aceites, pedido, ct);

        if (rotas.Erro is { } erroRotas)
            return erroRotas;

        // O custo exige uma distância positiva (EstimativaCustoService).
        if (rotas.Valor.Simulada.DistanciaTotalKm <= 0)
            return RotaNaoCalculada;

        // 3. Resposta: totais, desvio, ordem dos locais e a rota simulada para o mapa.
        return Montar(viagem, pedido, recolha, destino, aceites.Count + 1, rotas.Valor);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Passos: (1) validar o texto dos pontos e a viagem, sem mapas; (2) aplicar as regras do pedido real e obter as
    /// coordenadas (<see cref="PreparacaoDePedido.PrepararAsync"/>); (3) obter a rota atual e a rota com o pedido, como
    /// na simulação do condutor (<see cref="CalcularRotasAsync"/>); (4) montar a resposta só com os pontos que o
    /// passageiro pode ver (<see cref="SoPontosDoPassageiro"/>). O pedido nunca chega à base de dados.
    /// </remarks>
    public async Task<Resultado<SimulacaoNovoPedido>> SimularNovoPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        // 1. Texto dos pontos (P-20), viagem e condutor, sem chamar os mapas.
        if (PreparacaoDePedido.ValidarTextoDosPontos(recolha, destino) is { } pontosInvalidos)
            return pontosInvalidos;

        Viagem? viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        // O condutor vê os pedidos recebidos com POST /viagens/{id}/simulacao (RF-F004-09).
        if (viagem.CondutorId == passageiroId)
            return new Erro(TipoErro.Proibido, "O condutor simula os pedidos recebidos, não um pedido próprio.");

        // 2. As regras do pedido real: viagem por partir e com lugar, sem pedido repetido, pontos e coordenadas.
        Resultado<Participacao> preparado = await preparacao.PrepararAsync(viagem, passageiroId, recolha, destino, ct);

        if (preparado.Erro is { } recusado)
            return recusado;

        // O pedido não existe: com o id vazio, a mesma simulação repetida (por este ou outro passageiro) encontra a
        // rota em memória (D-2) e não volta a chamar os mapas.
        Participacao pedido = preparado.Valor;
        pedido.Id = Guid.Empty;

        // 3. Rota atual e rota com o pedido (rota guardada reutilizada; sem paragens novas, sem mapas).
        IReadOnlyList<Participacao> aceites = await repositorioParticipacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);
        Resultado<RotasDaSimulacao> rotas = await CalcularRotasAsync(viagem, aceites, pedido, ct);

        if (rotas.Erro is { } erroRotas)
            return erroRotas;

        if (rotas.Valor.Simulada.DistanciaTotalKm <= 0)
            return RotaNaoCalculada;

        // 4. Resposta com os pontos que o passageiro pode ver.
        return MontarParaPassageiro(viagem, pedido, aceites.Count + 1, rotas.Valor);
    }

    /// <summary>Verifica a viagem antes de simular: é do condutor que pede e está ativa.</summary>
    /// <param name="viagem">Viagem da simulação.</param>
    /// <param name="utilizadorId">Utilizador que pede a simulação.</param>
    /// <returns>O erro a devolver, ou <c>null</c> se a viagem permite simular.</returns>
    private static Erro? ValidarViagem(Viagem viagem, Guid utilizadorId)
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
    private static Erro? ValidarPedido(Viagem viagem, Participacao pedido)
    {
        if (!pedido.EstaPendente)
            return new Erro(TipoErro.Conflito, "Só é possível simular pedidos pendentes.");

        if (string.IsNullOrWhiteSpace(pedido.Recolha) && string.IsNullOrWhiteSpace(pedido.Destino))
            return new Erro(TipoErro.DadosInvalidos, "O pedido deve indicar recolha ou destino.");

        // Numa viagem cheia o pedido não pode ser aceite, por isso não há nada a simular (BE-26).
        if (!viagem.TemLugarLivre)
            return new Erro(TipoErro.Conflito, "Sem lugares disponíveis.");

        return null;
    }

    /// <summary>
    /// Obtém a rota atual (a guardada, ou calculada se estiver por recalcular) e a rota se o pedido for aceite.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Rota guardada e pedido sem paragens novas: as duas rotas são a guardada, sem chamar os mapas (BE-03).</item>
    /// <item>Pedido sem paragens novas e rota por recalcular: calcula-se uma vez e usa-se para as duas (BE-38 b).</item>
    /// <item>A matriz corre com o limite de 10 s (P-19); a linha da HERE junta-se depois, fora dele, e a rota simulada
    /// fica em memória 10 minutos para o aceitar seguinte a reutilizar sem chamar os mapas (D-2).</item>
    /// </list>
    /// </remarks>
    /// <returns>As duas rotas; ou o erro: 422 sem rota, 502, 503 ou 504.</returns>
    private async Task<Resultado<RotasDaSimulacao>> CalcularRotasAsync(
        Viagem viagem,
        IReadOnlyList<Participacao> aceites,
        Participacao pedido,
        CancellationToken ct)
    {
        ResultadoRotaComPontos? guardada = RotaGuardada.Ler(viagem);
        bool acrescentaParagens = RotaService.AcrescentaParagens(viagem, pedido);

        if (guardada is not null && !acrescentaParagens)
            return new RotasDaSimulacao(guardada, guardada);

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        Resultado<RotasDaSimulacao> calculadas = await ChamadaMapas.ExecutarAsync<RotasDaSimulacao>(
            async limite =>
            {
                // A rota atual é a guardada; só se calcula se estiver por recalcular (P-19). Nada é gravado.
                ResultadoRotaComPontos? original = guardada ?? await rotaService.CalcularAsync(viagem, aceites, limite);

                // BE-38 b: sem paragens novas, a rota com o pedido é a mesma; não se calcula duas vezes.
                ResultadoRotaComPontos? simulada = acrescentaParagens
                    ? await rotaService.CalcularAsync(viagem, [.. aceites, pedido], limite)
                    : original;

                if (original is null || simulada is null)
                    return RotaNaoCalculada;

                return new RotasDaSimulacao(original, simulada);
            },
            "Serviço de mapas indisponível.",
            MensagemTempo,
            ct);

        if (calculadas.Erro is { } erro)
            return erro;

        // D-2: a linha da HERE pede-se fora do limite de 10 s, e a simulação fica em memória para o aceitar.
        ResultadoRotaComPontos simuladaComLinha = await rotaService.ConcluirSimulacaoAsync(viagem, calculadas.Valor.Simulada, ct);

        return calculadas.Valor with { Simulada = simuladaComLinha };
    }

    /// <summary>Monta a resposta da simulação: a rota atual, a rota com o pedido e as diferenças (RF-F004-09).</summary>
    /// <param name="viagem">Viagem simulada.</param>
    /// <param name="pedido">Pedido simulado.</param>
    /// <param name="recolha">Recolha efetiva (a do pedido, ou a origem da viagem se for omitida).</param>
    /// <param name="destino">Destino efetivo (o do pedido, ou o destino da viagem se for omitido).</param>
    /// <param name="passageirosComOPedido">Passageiros aceites mais o do pedido (entram no custo por participante).</param>
    /// <param name="rotas">Rota atual e rota com o pedido.</param>
    private SimulacaoPedido Montar(
        Viagem viagem,
        Participacao pedido,
        string recolha,
        string destino,
        int passageirosComOPedido,
        RotasDaSimulacao rotas)
    {
        ResultadoRotaComPontos original = rotas.Original;
        ResultadoRotaComPontos simulada = rotas.Simulada;

        // BE-35 / D-2: o condutor vê todos os pontos (P-13) e a linha, no formato de GET /rotas/viagens/{id}.
        ResultadoEstimativaCusto custo = custos.Calcular(simulada.DistanciaTotalKm, passageirosComOPedido);
        RotaDaViagem rotaSimulada = RespostasDaRota.RotaCompleta(viagem.Id, passageirosComOPedido, simulada, custo);

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
            simulada.PontosOrdenados.Select(p => p.Local).ToList(),
            rotaSimulada);
    }

    /// <summary>Monta a resposta da simulação do passageiro: totais, desvio e a rota só com os seus pontos (P-13).</summary>
    /// <param name="viagem">Viagem simulada.</param>
    /// <param name="pedido">Pedido por enviar (id vazio).</param>
    /// <param name="passageirosComOPedido">Passageiros aceites mais o do pedido (entram no custo por participante).</param>
    /// <param name="rotas">Rota atual e rota com o pedido.</param>
    private SimulacaoNovoPedido MontarParaPassageiro(
        Viagem viagem,
        Participacao pedido,
        int passageirosComOPedido,
        RotasDaSimulacao rotas)
    {
        ResultadoRotaComPontos original = rotas.Original;
        ResultadoRotaComPontos simulada = rotas.Simulada;

        // Os totais e o custo são os da rota completa; os pontos e os troços, só os que o passageiro pode ver.
        ResultadoEstimativaCusto custo = custos.Calcular(simulada.DistanciaTotalKm, passageirosComOPedido);
        RotaDaViagem rotaSimulada = RespostasDaRota.RotaCompleta(
            viagem.Id,
            passageirosComOPedido,
            SoPontosDoPassageiro(simulada, pedido.Id),
            custo);

        return new SimulacaoNovoPedido(
            viagem.Id,
            pedido.Recolha ?? viagem.Origem,
            pedido.Destino ?? viagem.Destino,
            original.DistanciaTotalKm,
            original.DuracaoTotalMinutos,
            simulada.DistanciaTotalKm,
            simulada.DuracaoTotalMinutos,
            Math.Round(simulada.DistanciaTotalKm - original.DistanciaTotalKm, 2),
            simulada.DuracaoTotalMinutos - original.DuracaoTotalMinutos,
            rotaSimulada);
    }

    /// <summary>
    /// Tira da rota as paragens dos passageiros aceites (P-13, RF-F003-08, RF-F008-09): ficam a origem, o destino e os
    /// pontos do pedido simulado.
    /// </summary>
    /// <remarks>
    /// Os troços juntam-se entre os pontos que ficam (somam a distância e a duração dos troços escondidos), por isso os
    /// totais não mudam e nenhum troço aponta para um ponto escondido. A linha da HERE (<c>Geometria</c>) mantém-se: é o
    /// percurso real, que passa pelas paragens escondidas (SP-01, decisão a rever; guia §10). Sem linha da HERE, as retas
    /// ligam só os pontos que ficam.
    /// </remarks>
    /// <param name="rota">Rota simulada completa.</param>
    /// <param name="pedidoId">Id do pedido simulado (os seus pontos ficam).</param>
    /// <returns>A mesma rota, só com os pontos visíveis ao passageiro.</returns>
    private static ResultadoRotaComPontos SoPontosDoPassageiro(ResultadoRotaComPontos rota, Guid pedidoId)
    {
        PontoRota inicio = rota.PontosOrdenados[0];
        var pontos = new List<PontoRota> { inicio };
        var segmentos = new List<SegmentoRota>();
        decimal distancia = 0;
        int duracao = 0;

        foreach (SegmentoRota segmento in rota.Segmentos)
        {
            distancia += segmento.DistanciaKm;
            duracao += segmento.DuracaoMinutos;

            // A origem e o destino da viagem não têm participação; os pontos do pedido têm o id do pedido.
            bool visivel = segmento.Destino.ParticipacaoId is null || segmento.Destino.ParticipacaoId == pedidoId;

            if (!visivel)
                continue;

            segmentos.Add(new SegmentoRota(inicio, segmento.Destino, distancia, duracao));
            pontos.Add(segmento.Destino);
            inicio = segmento.Destino;
            distancia = 0;
            duracao = 0;
        }

        return rota with { PontosOrdenados = pontos, Segmentos = segmentos };
    }
}
