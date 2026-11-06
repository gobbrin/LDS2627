
namespace CampusCarpool.Application.Historico;

/// <summary>Histórico pessoal do utilizador autenticado (F009).</summary>
/// <remarks>
/// Implementado por <see cref="HistoricoService"/>.
/// Só o próprio utilizador consulta o seu histórico (RF-F009-07). Devolve tudo de uma vez, sem paginação (AP-10).
/// </remarks>
public interface IHistoricoService
{
    /// <summary>Devolve as seis listas do histórico do utilizador (RF-F009-01 a RF-F009-06).</summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct);
}
