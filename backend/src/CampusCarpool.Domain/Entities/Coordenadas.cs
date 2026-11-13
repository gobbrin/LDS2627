namespace CampusCarpool.Domain.Entities;

/// <summary>Coordenadas geográficas em graus decimais (WGS 84).</summary>
public sealed record Coordenadas(
    decimal Latitude,
    decimal Longitude);
