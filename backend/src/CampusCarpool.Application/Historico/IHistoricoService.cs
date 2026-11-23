
namespace CampusCarpool.Application.Historico;

/// <summary>Histórico pessoal do utilizador autenticado (F009).</summary>
/// <remarks>
/// Implementado por <see cref="HistoricoService"/>.
/// Só o próprio utilizador consulta o seu histórico (RF-F009-07). Cada lista é paginada (D-4; substitui o AP-10).
/// </remarks>
public interface IHistoricoService
{
    /// <summary>Devolve uma página de cada uma das seis listas do histórico do utilizador (RF-F009-01 a RF-F009-06).</summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="pagina">Página, a contar de 1 (validada pelo controller).</param>
    /// <param name="tamanho">Elementos por página em cada lista, de 1 a 100 (validado pelo controller).</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <returns>As seis listas da página pedida e os totais de cada lista.</returns>
    Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        int pagina,
        int tamanho,
        CancellationToken ct);
}
