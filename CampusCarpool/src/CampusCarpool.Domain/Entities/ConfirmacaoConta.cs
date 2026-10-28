namespace CampusCarpool.Domain.Entities;

public sealed class ConfirmacaoConta
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid UtilizadorId { get; set; }
    public string TokenHash { get; set; } = ""; public DateTime ExpiraEm { get; set; }
    public DateTime? ConsumidoEm { get; set; }
}
