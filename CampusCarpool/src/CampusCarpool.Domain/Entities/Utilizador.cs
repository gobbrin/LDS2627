namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Conta de um membro da comunidade académica (F001). O mesmo utilizador pode ser condutor e passageiro.
/// </summary>
/// <remarks>
/// O registo só aceita emails académicos, da lista configurada em <c>EmailAcademico:Dominios</c> (P-01).
/// Só contas confirmadas iniciam sessão (P-09).
/// Campos públicos do perfil (P-10): nome, curso, instituição, ligações externas e classificação média.
/// </remarks>
public sealed class Utilizador
{
    /// <summary>Identificador do utilizador (também a claim <c>sub</c> do JWT).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nome apresentado (obrigatório, até 100 caracteres, P-26).</summary>
    public string Nome { get; set; } = "";

    /// <summary>Email académico, único (RF-F001-04).</summary>
    public string Email { get; set; } = "";

    /// <summary>Hash da palavra-passe (nunca a palavra-passe em claro).</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>Verdadeiro depois de aberta a ligação de confirmação (RF-F001-07). O enum <c>EstadoConta</c> não é usado (DO-08).</summary>
    public bool EmailConfirmado { get; set; }

    /// <summary>Curso (opcional, público).</summary>
    public string? Curso { get; set; }

    /// <summary>Instituição (opcional, pública).</summary>
    public string? Instituicao { get; set; }

    /// <summary>Data de registo (UTC).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
