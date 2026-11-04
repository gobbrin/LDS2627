namespace CampusCarpool.Api.Services;

/// <summary>Ponto de uma rota.</summary>
/// <param name="ParticipacaoId">Participação a que o ponto pertence; <c>null</c> na origem e no destino da viagem.</param>
/// <param name="Tipo"><c>Origem</c>, <c>DestinoViagem</c>, <c>Recolha</c> ou <c>DestinoPassageiro</c> (texto, não enum).</param>
/// <param name="Local">Texto do local, tal como foi indicado.</param>
/// <param name="Coordenadas">Coordenadas geocodificadas.</param>
public sealed record PontoRota(
    Guid? ParticipacaoId,
    string Tipo,
    string Local,
    Coordenadas Coordenadas
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

