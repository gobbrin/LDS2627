namespace CampusCarpool.Domain.Entities;

public sealed class Utilizador
{
    public Guid Id { get; set; } = Guid.NewGuid(); public string Nome { get; set; } = ""; public string Email { get; set; } = ""; public string PasswordHash { get; set; } = ""; public bool EmailConfirmado { get; set; }
    public string? Curso { get; set; }
    public string? Instituicao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
