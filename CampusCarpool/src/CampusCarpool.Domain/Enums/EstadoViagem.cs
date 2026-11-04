namespace CampusCarpool.Domain.Enums;

/// <summary>Estados de uma viagem (P-04).</summary>
/// <remarks>
/// A coluna <c>Viagem.Estado</c> é texto: o código grava <c>nameof(EstadoViagem.X)</c>, por isso o valor
/// persistido é exatamente o nome do membro (<c>Concluida</c> sem acento). A restrição
/// <c>CK_Viagens_Estado</c> só aceita estes três valores.
/// </remarks>
public enum EstadoViagem { Ativa, Indisponibilizada, Concluida }
