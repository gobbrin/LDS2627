namespace CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

/// <summary>Notificação persistida e dirigida a um utilizador.</summary>
/// <remarks>
/// As notificações ficam armazenadas indefinidamente e são apresentadas por
/// ordem de chegada. Avisos do condutor são um dos tipos persistidos; não geram
/// push Android, de acordo com a declaração de âmbito.
/// </remarks>
public sealed class Notificacao
{
 public Guid Id {get;set;} = Guid.NewGuid(); public Guid UtilizadorId {get;set;} public Guid? ViagemId {get;set;} public TipoNotificacao Tipo {get;set;} public string Titulo {get;set;} = ""; public string Conteudo {get;set;} = ""; public DateTime? LidaEm {get;set;} public DateTime CriadaEm {get;set;} = DateTime.UtcNow;
}
