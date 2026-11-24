using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Participacoes;

/// <summary>Simulação do percurso de uma viagem com um pedido, sem gravar nada (RF-F004-09, RF-F008-06).</summary>
/// <remarks>Implementada por <see cref="SimulacaoDePedidoService"/>; chamada pelo <c>SimulacoesController</c>.</remarks>
public interface ISimulacaoDePedidoService
{
    /// <summary>Simula o percurso da viagem se o condutor aceitar o pedido indicado.</summary>
    /// <remarks>
    /// Não altera nada na base de dados. Compara a rota guardada com a rota que resultaria de juntar o pedido,
    /// calculada pelo mesmo algoritmo (<see cref="Rotas.IRotaService"/>, P-07), por isso a simulação coincide com a
    /// rota depois de aceitar. Pontos omitidos assumem a origem ou o destino da viagem (P-20). O cálculo tem um limite
    /// de 10 segundos; a linha da HERE pede-se fora dele (D-2). A rota simulada fica 10 minutos em memória, para o
    /// aceitar seguinte a reutilizar sem chamar os mapas.
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="pedidoId">Pedido pendente a simular.</param>
    /// <param name="utilizadorId">Utilizador autenticado; tem de ser o condutor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>
    /// A simulação, com a rota simulada para o mapa (<see cref="SimulacaoPedido.RotaSimulada"/>); ou o erro: 400 pedido
    /// sem pontos ou recolha igual ao destino, 403 não é o condutor, 404, 409 viagem não ativa, pedido não pendente ou
    /// viagem cheia, 422, 502, 503 ou 504.
    /// </returns>
    Task<Resultado<SimulacaoPedido>> SimularAsync(Guid viagemId, Guid pedidoId, Guid utilizadorId, CancellationToken ct);

    /// <summary>
    /// Simula, para o passageiro, a rota da viagem com um pedido que ele ainda não enviou (ecrã 14 do protótipo).
    /// </summary>
    /// <remarks>
    /// Aplica as regras do pedido real (<see cref="PreparacaoDePedido"/>), calcula como a simulação do condutor e não
    /// grava nada. A resposta só mostra a origem, o destino e os pontos do próprio passageiro (P-13, RF-F008-09).
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="passageiroId">Utilizador autenticado; não pode ser o condutor.</param>
    /// <param name="recolha">Ponto de recolha; <c>null</c> segue a origem da viagem (P-20).</param>
    /// <param name="destino">Ponto de destino; <c>null</c> segue o destino da viagem (P-20).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>
    /// A simulação; ou o erro: 400 pontos inválidos ou no mesmo local, 403 é o condutor, 404 viagem inexistente,
    /// 409 viagem indisponível, sem lugares ou pedido repetido (RF-F004-06), 422, 502, 503 ou 504.
    /// </returns>
    Task<Resultado<SimulacaoNovoPedido>> SimularNovoPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct);
}
