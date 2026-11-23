using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;

namespace CampusCarpool.Application.Viagens;

/// <summary>Simulação da alteração da origem ou do destino de uma viagem, sem gravar nada (RF-F002-07, RF-F008-05).</summary>
/// <remarks>Implementada por <see cref="SimulacaoDeAlteracaoService"/>; chamada pelo <c>ViagensController</c>.</remarks>
public interface ISimulacaoDeAlteracaoService
{
    /// <summary>
    /// Mostra ao condutor a rota, a distância, a duração e o custo que a viagem teria com a origem e o destino
    /// indicados, sem gravar nada (ecrã de edição; RF-F002-07, RF-F008-05, RF-F008-10).
    /// </summary>
    /// <remarks>
    /// <paramref name="origem"/> e <paramref name="destino"/> a <c>null</c> mantêm o valor atual, como em
    /// <see cref="IViagemService.EditarAsync"/>. A rota inclui as paragens dos passageiros aceites, como a que o PUT
    /// grava. Com os mesmos locais devolve a rota guardada sem chamar os mapas.
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="utilizadorId">Utilizador autenticado; tem de ser o condutor.</param>
    /// <param name="origem">Nova origem, ou <c>null</c> para manter a atual.</param>
    /// <param name="destino">Novo destino, ou <c>null</c> para manter o atual.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>
    /// A rota simulada, no formato de <c>GET /rotas/viagens/{id}</c>; erro 400 com locais inválidos (vazios, mais de
    /// 200 caracteres ou no mesmo local); 403 se não for o condutor; 404 se a viagem não existir; 409 se a viagem não
    /// for editável; 422 se um local ou a rota não forem encontrados; 502/503/504 com problemas no Serviço de Mapas.
    /// </returns>
    Task<Resultado<RotaDaViagem>> SimularAlteracaoAsync(
        Guid viagemId,
        Guid utilizadorId,
        string? origem,
        string? destino,
        CancellationToken ct);
}
