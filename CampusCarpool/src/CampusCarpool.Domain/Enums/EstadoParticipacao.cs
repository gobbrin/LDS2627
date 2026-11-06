namespace CampusCarpool.Domain.Enums;

/// <summary>Estados possíveis de um pedido de participação.</summary>
/// <remarks>
/// Transições em <see cref="Entities.Participacao"/> (AD-001 §5): Pendente para Aceite, Rejeitada ou Cancelada;
/// Aceite para Cancelada antes da partida ou com a viagem indisponibilizada (P-25).
/// </remarks>
public enum EstadoParticipacao { Pendente, Aceite, Rejeitada, Cancelada }
