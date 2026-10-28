namespace CampusCarpool.Domain.Enums;

/// <summary>Estados possíveis de um pedido de participação.</summary>
/// <remarks>
/// Transições permitidas: Pendente para Aceite, Rejeitada ou Cancelada;
/// Aceite para Cancelada antes da partida.
/// </remarks>
public enum EstadoParticipacao { Pendente, Aceite, Rejeitada, Cancelada }
