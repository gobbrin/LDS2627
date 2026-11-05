using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Troço entre dois pontos consecutivos, com distância (km) e duração (min).</summary>
public sealed record SegmentoRota(
    PontoRota Origem,
    PontoRota Destino,
    decimal DistanciaKm,
    int DuracaoMinutos
);

/// <summary>Rota completa: pontos por ordem, segmentos e totais (soma dos segmentos).</summary>
public sealed record ResultadoRotaComPontos(
    IReadOnlyList<PontoRota> PontosOrdenados,
    IReadOnlyList<SegmentoRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos
);
