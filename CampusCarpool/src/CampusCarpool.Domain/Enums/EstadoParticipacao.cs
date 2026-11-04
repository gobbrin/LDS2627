namespace CampusCarpool.Domain.Enums;

/// <summary>Estados possíveis de um pedido de participação.</summary>
/// <remarks>
/// Transições permitidas: Pendente para Aceite, Rejeitada ou Cancelada;
/// Aceite para Cancelada antes da partida.
/// Também passam a Cancelada os pendentes e aceites de uma viagem indisponibilizada (P-25) e os
/// pendentes de uma viagem concluída (RF-F004-19).
/// </remarks>
public enum EstadoParticipacao { Pendente, Aceite, Rejeitada, Cancelada }
