namespace CampusCarpool.Domain.Entities;

public sealed class Viagem
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid CondutorId { get; set; }
    public Utilizador Condutor { get; set; } = null!; public string Origem { get; set; } = ""; public string Destino { get; set; } = ""; public decimal? OrigemLatitude { get; set; }
    public decimal? OrigemLongitude { get; set; }
    public decimal? DestinoLatitude { get; set; }
    public decimal? DestinoLongitude { get; set; }
    public DateTime PartidaEm { get; set; }
    public int LugaresOferecidos { get; set; }
    public int LugaresDisponiveis { get; set; }
    public string Estado { get; set; } = "Ativa"; public decimal? DistanciaKm { get; set; }
    public int? DuracaoMinutos { get; set; }

    public byte[] Versao { get; set; } = Array.Empty<byte>();
}
