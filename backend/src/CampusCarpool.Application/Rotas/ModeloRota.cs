using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

// Modelo interno da rota: usado pelo cálculo, pela gravação em Viagem.RotaPontos e pela simulação. Não sai na API;
// as respostas usam os tipos de DtosRotas.cs (BE-06).

/// <summary>Troço entre dois pontos consecutivos, com distância (km) e duração (min).</summary>
public sealed record SegmentoRota(
    PontoRota Origem,
    PontoRota Destino,
    decimal DistanciaKm,
    int DuracaoMinutos
);

/// <summary>Rota completa: pontos por ordem, segmentos e totais (soma dos segmentos).</summary>
/// <remarks>
/// <c>Geometria</c> é a linha da HERE (DEF-BE-054); <c>null</c> enquanto não for pedida, ou se a HERE não a deu.
/// Nesse caso o mapa mostra linhas retas entre os pontos (<see cref="GeometriaRota.LinhaReta"/>).
/// </remarks>
public sealed record ResultadoRotaComPontos(
    IReadOnlyList<PontoRota> PontosOrdenados,
    IReadOnlyList<SegmentoRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos,
    IReadOnlyList<Coordenadas>? Geometria = null
);
