namespace CampusCarpool.Domain.Entities;

/// <summary>Tipos de ponto de uma rota.</summary>
public static class TiposPontoRota
{
    /// <summary>Origem da viagem.</summary>
    public const string Origem = "Origem";
    /// <summary>Destino da viagem.</summary>
    public const string DestinoViagem = "DestinoViagem";
    /// <summary>Recolha de um passageiro.</summary>
    public const string Recolha = "Recolha";
    /// <summary>Destino de um passageiro.</summary>
    public const string DestinoPassageiro = "DestinoPassageiro";
}

/// <summary>Ponto de uma rota.</summary>
/// <param name="ParticipacaoId">Participação a que o ponto pertence; <c>null</c> na origem e no destino da viagem.</param>
/// <param name="Tipo">Um dos valores de <see cref="TiposPontoRota"/>.</param>
/// <param name="Local">Texto do local, tal como foi indicado.</param>
/// <param name="Coordenadas">Coordenadas geocodificadas.</param>
public sealed record PontoRota(
    Guid? ParticipacaoId,
    string Tipo,
    string Local,
    Coordenadas Coordenadas
)
{
    /// <summary>Origem da viagem como ponto da rota.</summary>
    public static PontoRota OrigemDe(Viagem viagem)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return new PontoRota(
            null,
            TiposPontoRota.Origem,
            viagem.Origem,
            new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude));
    }

    /// <summary>Destino da viagem como ponto da rota.</summary>
    public static PontoRota DestinoDe(Viagem viagem)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return new PontoRota(
            null,
            TiposPontoRota.DestinoViagem,
            viagem.Destino,
            new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude));
    }
}
