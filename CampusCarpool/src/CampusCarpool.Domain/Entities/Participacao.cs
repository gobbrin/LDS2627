using System.Text.Json.Serialization;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Pedido de um passageiro para participar numa viagem.
/// </summary>
/// <remarks>
/// O estado inicial é <see cref="EstadoParticipacao.Pendente"/>.
/// Apenas participações <see cref="EstadoParticipacao.Aceite"/> ocupam lugares.
/// A conversão EF mantém o estado persistido como texto para compatibilidade
/// com o esquema existente.
/// </remarks>
public sealed class Participacao
{
 public Guid Id {get;set;} = Guid.NewGuid(); public Guid ViagemId {get;set;} public Viagem Viagem {get;set;} = null!; public Guid PassageiroId {get;set;} public Utilizador Passageiro {get;set;} = null!; [JsonConverter(typeof(JsonStringEnumConverter))] public EstadoParticipacao Estado {get;set;} = EstadoParticipacao.Pendente; public string? Recolha {get;set;} public string? Destino {get;set;} public DateTime CriadoEm {get;set;} = DateTime.UtcNow;
}
