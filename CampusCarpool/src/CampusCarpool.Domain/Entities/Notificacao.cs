namespace CampusCarpool.Domain.Entities;

using CampusCarpool.Domain.Enums;

/// <summary>Notificação persistida e dirigida a um utilizador (F006).</summary>
/// <remarks>
/// As notificações ficam armazenadas indefinidamente e são apresentadas por
/// ordem de chegada. Avisos do condutor são um dos tipos persistidos; não geram
/// push Android, de acordo com a declaração de âmbito.
/// O título e o conteúdo são copiados no momento da criação, por isso a notificação continua legível
/// mesmo que a origem (aviso, viagem) mude depois (DO-13). Os tipos que geram push são decididos em
/// <c>NotificacaoService</c> (RF-F006-12, RF-F006-16, RF-F006-17).
/// </remarks>
public sealed class Notificacao
{
    /// <summary>Identificador da notificação.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Destinatário.</summary>
    public Guid UtilizadorId { get; set; }

    /// <summary>Viagem relacionada, quando existe.</summary>
    public Guid? ViagemId { get; set; }

    /// <summary>Tipo da notificação (ver <see cref="TipoNotificacao"/>).</summary>
    public TipoNotificacao Tipo { get; set; }

    /// <summary>Título apresentado ao utilizador.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Texto apresentado ao utilizador.</summary>
    public string Conteudo { get; set; } = "";

    /// <summary>Momento (UTC) em que o utilizador a marcou como lida; <c>null</c> se ainda não foi lida.</summary>
    public DateTime? LidaEm { get; set; }

    /// <summary>Data de criação (UTC).</summary>
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
