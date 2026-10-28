namespace CampusCarpool.Domain.Entities;

public sealed class Dispositivo
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid UtilizadorId { get; set; }
    public string Token { get; set; } = ""; public bool Ativo { get; set; } = true;
}
