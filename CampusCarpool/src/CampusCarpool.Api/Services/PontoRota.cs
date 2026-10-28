namespace CampusCarpool.Api.Services;

public sealed record PontoRota(
    Guid? ParticipacaoId,
    string Tipo,
    string Local,
    Coordenadas Coordenadas
);

public sealed record SegmentoRota(
    PontoRota Origem,
    PontoRota Destino,
    decimal DistanciaKm,
    int DuracaoMinutos
);

public sealed record ResultadoRotaComPontos(
    IReadOnlyList<PontoRota> PontosOrdenados,
    IReadOnlyList<SegmentoRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos
);

