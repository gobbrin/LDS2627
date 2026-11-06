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
    public string Nome { get; internal set; } = "";

    /// <summary>Email académico, único (RF-F001-04).</summary>
    public string Email { get; set; } = "";

    /// <summary>Hash da palavra-passe (nunca a palavra-passe em claro).</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>Verdadeiro depois de aberta a ligação de confirmação (RF-F001-07).</summary>
    public bool EmailConfirmado { get; internal set; }

    /// <summary>Curso (opcional, público).</summary>
    public string? Curso { get; internal set; }

    /// <summary>Instituição (opcional, pública).</summary>
    public string? Instituicao { get; internal set; }

    /// <summary>Data de registo (UTC).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Cria uma conta por confirmar (RF-F001-02).</summary>
    /// <param name="nome">Nome apresentado.</param>
    /// <param name="email">Email académico, já normalizado.</param>
    public static Utilizador Registar(string nome, string email) => new() { Nome = nome, Email = email };

    /// <summary>Confirma a conta (RF-F001-07).</summary>
    public void Confirmar() => EmailConfirmado = true;

    /// <summary>Altera os dados do perfil (RF-F001-14).</summary>
    /// <param name="nome">Nome.</param>
    /// <param name="curso">Curso, ou <c>null</c>.</param>
    /// <param name="instituicao">Instituição, ou <c>null</c>.</param>
    public void AtualizarPerfil(string nome, string? curso, string? instituicao)
    {
        Nome = nome;
        Curso = curso;
        Instituicao = instituicao;
    }
}
