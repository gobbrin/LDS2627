namespace CampusCarpool.Domain.Enums;

/// <summary>Estados previstos de uma conta.</summary>
/// <remarks>
/// Não é usado: o <c>Utilizador</c> guarda apenas <c>EmailConfirmado</c>, e <c>Indisponivel</c> não tem
/// requisito (DO-08, fica para m4).
/// </remarks>
public enum EstadoConta { NaoConfirmada, Confirmada, Indisponivel }
