namespace CampusCarpool.Domain.Entities;

public sealed class LigacaoExterna
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid UtilizadorId { get; set; }
    public string Url { get; set; } = ""; public string? Descricao { get; set; }
    public int Ordem { get; set; }
}
