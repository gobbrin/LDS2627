namespace CampusCarpool.Domain.Entities;

/// <summary>Origem e destino de uma viagem, com as coordenadas obtidas pelo Serviço de Mapas (RF-F008-01).</summary>
/// <param name="Origem">Texto da origem.</param>
/// <param name="CoordenadasOrigem">Coordenadas da origem.</param>
/// <param name="Destino">Texto do destino.</param>
/// <param name="CoordenadasDestino">Coordenadas do destino.</param>
public sealed record LocaisViagem(
    string Origem,
    Coordenadas CoordenadasOrigem,
    string Destino,
    Coordenadas CoordenadasDestino);
