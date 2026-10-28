namespace CampusCarpool.Domain.Entities;

public sealed class Aviso
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid ViagemId { get; set; }
    public Guid CondutorId { get; set; }
    public string Conteudo { get; set; } = ""; public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
