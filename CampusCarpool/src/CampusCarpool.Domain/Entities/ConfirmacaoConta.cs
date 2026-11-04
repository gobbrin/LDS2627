namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Token de confirmação de conta enviado por email depois do registo (RF-F001-06 a RF-F001-08).
/// </summary>
/// <remarks>
/// Só se guarda o hash do token; o token em claro vai apenas na ligação do email.
/// A ligação é válida durante 24 horas (P-02) e só pode ser usada uma vez.
/// </remarks>
public sealed class ConfirmacaoConta
{
    /// <summary>Identificador do registo de confirmação.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Utilizador cuja conta é confirmada.</summary>
    public Guid UtilizadorId { get; set; }

    /// <summary>Hash do token enviado por email.</summary>
    public string TokenHash { get; set; } = "";

    /// <summary>Momento (UTC) a partir do qual a ligação deixa de ser aceite.</summary>
    public DateTime ExpiraEm { get; set; }

    /// <summary>Momento (UTC) em que a ligação foi usada; <c>null</c> enquanto não for usada.</summary>
    public DateTime? ConsumidoEm { get; set; }
}
