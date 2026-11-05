using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Api.Services;

/// <summary>Pede a matriz ao Serviço de Mapas e monta a rota pela ordem do <see cref="OrdenadorDePontos"/>.</summary>
public static class OtimizadorRotas
{
    /// <summary>Calcula a rota com os pontos adicionais entre a origem e o destino da viagem.</summary>
    /// <remarks>Faz um único pedido à Matrix API, com todas as durações e distâncias.</remarks>
    /// <returns>A rota; <c>null</c> se o serviço não devolver matriz ou não houver nenhuma sequência viável.</returns>
    /// <exception cref="ArgumentException">Mais pontos adicionais do que os possíveis numa viagem.</exception>
    public static async Task<ResultadoRotaComPontos?> CalcularAsync(
        IServicoMapasRotas mapas,
        PontoRota origem,
        PontoRota destino,
        IReadOnlyList<PontoRota> adicionais,
        CancellationToken ct)
    {
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
}
