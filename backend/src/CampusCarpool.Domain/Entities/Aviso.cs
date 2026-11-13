namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Aviso enviado pelo condutor aos passageiros aceites de uma viagem ativa (RF-F005-05).
/// </summary>
/// <remarks>
/// Cada aviso gera também uma <see cref="Notificacao"/> do tipo <c>Aviso</c> por passageiro aceite
/// (RF-F006-05), com cópia do texto e sem push Android (RF-F006-17). Limite de 100 caracteres (P-28),
/// validado em <c>AvisoService</c>.
/// </remarks>
public sealed class Aviso
{
    /// <summary>Identificador do aviso.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Viagem a que o aviso se refere.</summary>
    public Guid ViagemId { get; set; }

    /// <summary>Condutor que enviou o aviso (tem de ser o condutor da viagem, RF-F005-07).</summary>
    public Guid CondutorId { get; set; }

    /// <summary>Texto do aviso.</summary>
    public string Conteudo { get; set; } = "";

    /// <summary>Data de envio (UTC).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
