using System.Globalization;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Domain.Rules;
using Microsoft.Extensions.Caching.Memory;

namespace CampusCarpool.Application.Rotas;

/// <summary>
/// Rota guardada de uma viagem, com os pontos das participações aceites (RF-F008-03 a RF-F008-05, D-025).
/// </summary>
/// <remarks>
/// A rota é calculada ao criar e alterar a viagem e ao aceitar ou cancelar uma participação, e fica em
/// <see cref="Viagem.RotaPontos"/>; a consulta lê-a sem chamar o Serviço de Mapas. <c>RotaPontos</c> a <c>null</c>
/// significa «por recalcular»: acontece quando um cancelamento não conseguiu recalcular (P-19), e a rota é
/// recalculada na consulta seguinte em que o serviço responda (só numa viagem ativa, BE-38 d).
/// <para>A linha do mapa (DEF-BE-054) junta-se à rota fora do limite de 10 s dos mapas (<see cref="JuntarLinhaAsync"/>):
/// vem da HERE (<see cref="IServicoGeometriaRotas"/>) e reutiliza-se enquanto os pontos não mudarem.</para>
/// <para>Simulações (D-2): a rota simulada, já com a linha, fica em memória durante <see cref="ValidadeDaSimulacao"/>
/// (<see cref="ConcluirSimulacaoAsync"/>). Se a gravação seguinte (aceitar o pedido, guardar a alteração) tiver os
/// mesmos pontos, <see cref="CalcularAsync(PontoRota, PontoRota, IReadOnlyList{PontoRota}, CancellationToken)"/>
/// devolve essa rota sem chamar os mapas. As simulações nunca vão para a base de dados.</para>
/// </remarks>
public sealed class RotaService(
    IServicoMapasRotas mapas,
    IServicoGeometriaRotas geometria,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IMemoryCache simulacoes) : IRotaService
{
    /// <summary>Tempo durante o qual uma rota simulada pode ser reutilizada ao gravar (D-2).</summary>
    public static readonly TimeSpan ValidadeDaSimulacao = TimeSpan.FromMinutes(10);

    private const string TempoExcedido = "Tempo de resposta do serviço de mapas excedido.";

    /// <inheritdoc />
    public Task<ResultadoRotaComPontos?> CalcularAsync(
        Viagem viagem,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return CalcularEntreAsync(PontoRota.OrigemDe(viagem), PontoRota.DestinoDe(viagem), participacoes, ct);
    }

    /// <inheritdoc />
    public Task<ResultadoRotaComPontos?> CalcularEntreAsync(
        PontoRota origem,
        PontoRota destino,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(origem);
        ArgumentNullException.ThrowIfNull(destino);
        ArgumentNullException.ThrowIfNull(participacoes);

        var paragens = new List<PontoRota>();

        foreach (Participacao participacao in participacoes)
            paragens.AddRange(ParagensDe(participacao, origem.Coordenadas, destino.Coordenadas));

        return CalcularAsync(origem, destino, paragens, ct);
    }

    /// <summary>Indica se a participação acrescenta paragens à rota da viagem (decisão de 16/11, BE-03).</summary>
    /// <remarks>
    /// Um ponto omitido segue a origem ou o destino da viagem (P-20), e um ponto com as mesmas coordenadas que a origem
    /// ou o destino também não é paragem nova. Sem paragens novas a rota guardada continua certa: aceitar, cancelar ou
    /// simular essa participação não chama o Serviço de Mapas.
    /// </remarks>
    /// <param name="viagem">Viagem, com as coordenadas da origem e do destino.</param>
    /// <param name="participacao">Pedido ou participação aceite.</param>
    /// <returns><c>true</c> se a rota tem de ser recalculada por causa desta participação.</returns>
    public static bool AcrescentaParagens(Viagem viagem, Participacao participacao)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(participacao);

        var origem = new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude);
        var destino = new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude);

        return ParagensDe(participacao, origem, destino).Count > 0;
    }

    /// <summary>Calcula a rota com os pontos adicionais entre a origem e o destino da viagem.</summary>
    /// <remarks>
    /// Primeiro procura uma simulação recente com os mesmos pontos (D-2): se existir, devolve-a, com a linha, sem chamar
    /// os mapas. Senão faz um único pedido de matriz (durações e distâncias entre todos os pontos) e ordena os pontos
    /// com o <see cref="OrdenadorDePontos"/> (P-07).
    /// </remarks>
    /// <returns>A rota; <c>null</c> se o serviço não devolver matriz ou não houver nenhuma sequência viável.</returns>
    /// <exception cref="ArgumentException">Mais pontos adicionais do que os possíveis numa viagem.</exception>
    public async Task<ResultadoRotaComPontos?> CalcularAsync(
        PontoRota origem,
        PontoRota destino,
        IReadOnlyList<PontoRota> adicionais,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(adicionais);
        OrdenadorDePontos.GarantirLimite(adicionais);

        // D-2: uma simulação feita há pouco com os mesmos pontos já tem a rota e a linha.
        if (simulacoes.TryGetValue(ChaveDaSimulacao(origem, destino, adicionais), out ResultadoRotaComPontos? simulada))
            return simulada;

        var pontos = new List<PontoRota>(adicionais.Count + 2) { origem };
        pontos.AddRange(adicionais);
        pontos.Add(destino);

        var matriz = await mapas.CalcularMatrizAsync(pontos.Select(p => p.Coordenadas).ToList(), ct);
        if (matriz is null) return null;

        var ordem = OrdenadorDePontos.Ordenar(matriz.DuracoesSegundos, adicionais);
        if (ordem is null) return null;

        var ordenados = ordem.Select(i => pontos[i]).ToList();
        var segmentos = new List<SegmentoRota>(ordenados.Count - 1);

        for (var i = 0; i < ordem.Count - 1; i++)
        {
            var metros = matriz.DistanciasMetros[ordem[i]][ordem[i + 1]];
            var segundos = matriz.DuracoesSegundos[ordem[i]][ordem[i + 1]];
            if (metros is null || segundos is null) return null;

            segmentos.Add(new SegmentoRota(
                ordenados[i],
                ordenados[i + 1],
                Math.Round((decimal)(metros.Value / 1000d), 2),
                (int)Math.Ceiling(segundos.Value / 60d)));
        }

        return new ResultadoRotaComPontos(
            ordenados,
            segmentos,
            segmentos.Sum(s => s.DistanciaKm),
            segmentos.Sum(s => s.DuracaoMinutos));
    }

    /// <inheritdoc />
    public async Task<bool> RecalcularAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        if (!mapas.Configurado) return false;

        // O cálculo passa pela ChamadaMapas, com o limite de 10 s (P-19); qualquer falha devolve false.
        Resultado<ResultadoRotaComPontos?> calculada = await ChamadaMapas.ExecutarAsync<ResultadoRotaComPontos?>(
            async limite => await CalcularAsync(viagem, aceites, limite),
            "Serviço de mapas indisponível.",
            TempoExcedido,
            ct);

        if (calculada.Erro is not null || calculada.Valor is null)
            return false;

        // A linha da HERE fica fora do limite: tem o seu e nunca faz falhar a operação.
        await GuardarAsync(viagem, calculada.Valor, ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<Resultado<ResultadoRotaComPontos>> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        var guardada = RotaGuardada.Ler(viagem);
        if (guardada is not null) return guardada;

        // BE-38 d: uma viagem indisponibilizada ou concluída já não se realiza; não se gastam pedidos aos mapas.
        if (!viagem.EstaAtiva)
            return new Erro(TipoErro.NaoProcessavel, "A viagem já não está ativa: a rota não é recalculada.");

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        // BE-36: só a matriz corre dentro do limite de 10 s; a linha da HERE pede-se depois, fora dele.
        Resultado<ResultadoRotaComPontos?> calculada = await ChamadaMapas.ExecutarAsync<ResultadoRotaComPontos?>(
            async limite => await CalcularAsync(viagem, aceites, limite),
            "Serviço de mapas indisponível.",
            TempoExcedido,
            ct);

        if (calculada.Erro is { } erro)
            return erro;

        if (calculada.Valor is null)
            return new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

        ResultadoRotaComPontos rota = await GuardarAsync(viagem, calculada.Valor, ct);

        try
        {
            await unidadeDeTrabalho.ConfirmarAsync(ct);
        }
        catch (RegraDeNegocioException)
        {
            // Outra operação mudou a viagem entretanto: esta consulta responde na mesma e a seguinte recalcula.
        }

        return rota;
    }

    /// <inheritdoc />
    public async Task<ResultadoRotaComPontos> GuardarAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
    {
        ResultadoRotaComPontos comLinha = await JuntarLinhaAsync(viagem, rota, ct);
        RotaGuardada.Guardar(viagem, comLinha);

        return comLinha;
    }

    /// <inheritdoc />
    public async Task<ResultadoRotaComPontos> JuntarLinhaAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(rota);

        // A rota já traz linha (de uma simulação recente, D-2, ou da rota direta da HERE): fica essa.
        if (rota.Geometria is not null)
            return rota;

        // Decisão de 16/11: se os pontos não mudaram, a linha guardada continua certa e não se chama a HERE.
        ResultadoRotaComPontos? anterior = RotaGuardada.Ler(viagem);

        if (anterior?.Geometria is not null && GeometriaRota.MesmosPontos(anterior, rota))
            return rota with { Geometria = anterior.Geometria };

        IReadOnlyList<Coordenadas>? linha = await PedirLinhaAsync(GeometriaRota.LinhaReta(rota.PontosOrdenados), ct);

        return rota with { Geometria = linha };
    }

    /// <inheritdoc />
    public async Task<ResultadoRotaComPontos> ConcluirSimulacaoAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
    {
        ResultadoRotaComPontos comLinha = await JuntarLinhaAsync(viagem, rota, ct);

        // Fica em memória (nunca na BD) para a gravação seguinte com os mesmos pontos não chamar os mapas (D-2).
        string chave = ChaveDaSimulacao(
            comLinha.PontosOrdenados[0],
            comLinha.PontosOrdenados[^1],
            comLinha.PontosOrdenados.Skip(1).Take(comLinha.PontosOrdenados.Count - 2).ToList());

        var opcoes = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ValidadeDaSimulacao, Size = 1 };
        simulacoes.Set(chave, comLinha, opcoes);

        return comLinha;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Coordenadas>?> PedirLinhaAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct)
    {
        // Sem chave da HERE não se faz o pedido: o mapa mostra linhas retas (a reserva).
        if (!geometria.Configurado)
            return null;

        return await geometria.CalcularLinhaAsync(pontos, ct);
    }

    /// <summary>Chave de uma simulação na memória: os pontos de partida do cálculo, sem depender da sua ordem.</summary>
    /// <remarks>
    /// A origem e o destino vêm primeiro; as paragens ordenam-se pelo texto, porque o resultado do cálculo as reordena.
    /// Cada ponto leva o tipo, a participação, o local e as coordenadas: dois cálculos com a mesma chave dão a mesma rota.
    /// </remarks>
    private static string ChaveDaSimulacao(PontoRota origem, PontoRota destino, IReadOnlyList<PontoRota> adicionais)
    {
        var paragens = new List<string>();

        foreach (PontoRota paragem in adicionais)
            paragens.Add(TextoDoPonto(paragem));

        paragens.Sort(StringComparer.Ordinal);

        return "simulacao|" + TextoDoPonto(origem) + "|" + TextoDoPonto(destino) + "|" + string.Join("|", paragens);
    }

    // Texto de um ponto para a chave: tipo, participação, local e coordenadas. As coordenadas vão com até 6 casas
    // (a precisão da BD), para 41.23 e 41.230000 (como o SQL Server as devolve) darem a mesma chave.
    private static string TextoDoPonto(PontoRota ponto) =>
        ponto.Tipo + ";" + ponto.ParticipacaoId + ";" + ponto.Local + ";"
        + ponto.Coordenadas.Latitude.ToString("0.######", CultureInfo.InvariantCulture) + ";"
        + ponto.Coordenadas.Longitude.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>Paragens que a participação acrescenta entre a origem e o destino da rota.</summary>
    /// <remarks>
    /// Um ponto omitido segue a origem ou o destino da viagem (P-20). Um ponto nas mesmas coordenadas que a origem ou o
    /// destino (por exemplo, a recolha com o texto da origem, BE-03) não é paragem: a rota não muda por causa dele.
    /// </remarks>
    /// <returns>A recolha e o destino do passageiro que são locais novos; lista vazia se não houver nenhum.</returns>
    private static List<PontoRota> ParagensDe(Participacao participacao, Coordenadas origem, Coordenadas destino)
    {
        var paragens = new List<PontoRota>();

        if (!string.IsNullOrWhiteSpace(participacao.Recolha))
        {
            Coordenadas recolha = CoordenadasDe(participacao.RecolhaLatitude, participacao.RecolhaLongitude, participacao.Id);

            if (recolha != origem && recolha != destino)
                paragens.Add(new PontoRota(participacao.Id, TiposPontoRota.Recolha, participacao.Recolha, recolha));
        }

        if (!string.IsNullOrWhiteSpace(participacao.Destino))
        {
            Coordenadas fim = CoordenadasDe(participacao.DestinoLatitude, participacao.DestinoLongitude, participacao.Id);

            if (fim != origem && fim != destino)
                paragens.Add(new PontoRota(participacao.Id, TiposPontoRota.DestinoPassageiro, participacao.Destino, fim));
        }

        return paragens;
    }

    // O pedido guarda sempre as coordenadas do ponto indicado (A8); faltarem é um erro de dados.
    private static Coordenadas CoordenadasDe(decimal? latitude, decimal? longitude, Guid participacaoId) =>
        latitude is decimal lat && longitude is decimal lon
            ? new Coordenadas(lat, lon)
            : throw new InvalidOperationException($"Participação {participacaoId} sem coordenadas.");
}
