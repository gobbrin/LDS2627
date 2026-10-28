namespace CampusCarpool.Domain.Entities;

public sealed class Mensagem
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid? ViagemId { get; set; }
    public Guid RemetenteId { get; set; }
    public Guid DestinatarioId { get; set; }
    public string Conteudo { get; set; } = ""; public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
}
