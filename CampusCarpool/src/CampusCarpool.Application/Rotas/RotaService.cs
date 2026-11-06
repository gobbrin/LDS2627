using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Rotas;

/// <summary>
/// Rota guardada de uma viagem, com os pontos das participações aceites (RF-F008-03 a RF-F008-05, D-025).
/// </summary>
/// <remarks>
/// A rota é calculada ao criar e alterar a viagem e ao aceitar ou cancelar uma participação, e fica em
/// <see cref="Viagem.RotaPontos"/>; a consulta lê-a sem chamar o Serviço de Mapas. <c>RotaPontos</c> a <c>null</c>
/// significa «por recalcular»: acontece quando um cancelamento não conseguiu recalcular (P-19), e a rota é
/// recalculada na consulta seguinte em que o serviço responda.
/// </remarks>
public sealed class RotaService(IServicoMapasRotas mapas, IUnidadeDeTrabalho unidadeDeTrabalho) : IRotaService
{
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
        ArgumentNullException.ThrowIfNull(participacoes);

        return CalcularAsync(origem, destino, participacoes.SelectMany(PontosDe).ToList(), ct);
    }

    /// <summary>Calcula a rota com os pontos adicionais entre a origem e o destino da viagem.</summary>
    /// <remarks>Faz um único pedido à Matrix API, com todas as durações e distâncias.</remarks>
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

        try
        {
            var rota = await CalcularAsync(viagem, aceites, ct);
            if (rota is null) return false;

            RotaGuardada.Guardar(viagem, rota);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ResultadoRotaComPontos?> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        var guardada = RotaGuardada.Ler(viagem);
        if (guardada is not null) return guardada;

        var rota = await CalcularAsync(viagem, aceites, ct);
        if (rota is null) return null;

        RotaGuardada.Guardar(viagem, rota);

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

    // Um ponto omitido segue a origem ou o destino da viagem (P-20), por isso não acrescenta paragens.
    private static IEnumerable<PontoRota> PontosDe(Participacao participacao)
    {
        if (!string.IsNullOrWhiteSpace(participacao.Recolha))
        {
            yield return new PontoRota(
                participacao.Id,
                TiposPontoRota.Recolha,
                participacao.Recolha,
                CoordenadasDe(participacao.RecolhaLatitude, participacao.RecolhaLongitude, participacao.Id));
        }

        if (!string.IsNullOrWhiteSpace(participacao.Destino))
        {
            yield return new PontoRota(
                participacao.Id,
                TiposPontoRota.DestinoPassageiro,
                participacao.Destino,
                CoordenadasDe(participacao.DestinoLatitude, participacao.DestinoLongitude, participacao.Id));
        }
    }

    // O pedido guarda sempre as coordenadas do ponto indicado (A8); faltarem é um erro de dados.
    private static Coordenadas CoordenadasDe(decimal? latitude, decimal? longitude, Guid participacaoId) =>
        latitude is decimal lat && longitude is decimal lon
            ? new Coordenadas(lat, lon)
            : throw new InvalidOperationException($"Participação {participacaoId} sem coordenadas.");
}
