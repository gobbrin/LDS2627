using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Rota de uma viagem, tal como o utilizador a pode ver, com a estimativa de custo.</summary>
public sealed record RotaDaViagem(
    Guid ViagemId,
    int ParticipacoesAceites,
    IReadOnlyList<PontoRota> PontosOrdenados,
    IReadOnlyList<SegmentoRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos,
    ResultadoEstimativaCusto EstimativaCusto);

/// <summary>Estimativa de custo devolvida ao cliente (RF-F008-10, RF-F008-11).</summary>
/// <remarks><c>Indicativo</c> é sempre <c>true</c>: o valor é só uma referência para dividir despesas, não um preço.</remarks>
public sealed record ResultadoEstimativaCusto(
    decimal DistanciaKm,
    decimal PrecoPorKm,
    int PassageirosAceites,
    int TotalParticipantes,
    decimal CustoTotalEstimado,
    decimal CustoPorParticipante,
    bool Indicativo,
    string Moeda
);

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
