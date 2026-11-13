namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Mensagem individual entre dois utilizadores confirmados (F005, RF-F005-01).
/// </summary>
/// <remarks>
/// Uma conversa é o conjunto das mensagens trocadas entre o mesmo par de utilizadores; só os dois
/// intervenientes lhe têm acesso (RF-F005-04). Limite de 1000 caracteres (P-14), validado em
/// <c>MensagemService</c>. Cada mensagem gera uma notificação <c>NovaMensagem</c>, sem push (RF-F006-12).
/// </remarks>
public sealed class Mensagem
{
    /// <summary>Identificador da mensagem.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Viagem de contexto, opcional (a mensagem pode não estar ligada a nenhuma viagem).</summary>
    public Guid? ViagemId { get; set; }

    /// <summary>Autor da mensagem.</summary>
    public Guid RemetenteId { get; set; }

    /// <summary>Destinatário da mensagem.</summary>
    public Guid DestinatarioId { get; set; }

    /// <summary>Texto da mensagem.</summary>
    public string Conteudo { get; set; } = "";

    /// <summary>Data de envio (UTC); a conversa é ordenada por este campo (RF-F005-09).</summary>
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
}
