namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Ligação do perfil a um perfil externo, por exemplo LinkedIn ou GitHub (RF-F001-15, RF-F001-16).
/// </summary>
/// <remarks>
/// O URL tem de ser absoluto, com esquema http ou https; cada utilizador tem até 5 ligações (P-27).
/// As regras estão no <c>UtilizadoresController</c>.
/// </remarks>
public sealed class LigacaoExterna
{
    /// <summary>Identificador da ligação.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Dono do perfil.</summary>
    public Guid UtilizadorId { get; set; }

    /// <summary>URL absoluto (http ou https).</summary>
    public string Url { get; set; } = "";

    /// <summary>Texto opcional a mostrar em vez do URL.</summary>
    public string? Descricao { get; set; }

    /// <summary>Posição da ligação na lista do perfil.</summary>
    public int Ordem { get; set; }
}
