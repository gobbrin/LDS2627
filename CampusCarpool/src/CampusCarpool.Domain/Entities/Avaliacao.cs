namespace CampusCarpool.Domain.Entities;

public sealed class Avaliacao
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid ViagemId { get; set; }
    public Guid AvaliadorId { get; set; }
    public Guid AvaliadoId { get; set; }
    public byte Classificacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
