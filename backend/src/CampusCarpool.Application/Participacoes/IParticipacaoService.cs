using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Participacoes;

/// <summary>
/// Contrato usado pela API para gerir participações sem conhecer a base de dados.
/// </summary>
/// <remarks>
/// Implementado por <see cref="ParticipacaoService"/>. O evento em tempo real e o push saem depois de a operação
/// ser confirmada, para não anunciar uma alteração que não ficou gravada.
/// </remarks>
public interface IParticipacaoService
{
    /// <summary>Submete um pedido de participação, no estado <c>Pendente</c> (RF-F004-01 a RF-F004-07, RF-F004-13).</summary>
    /// <remarks>
    /// Não reserva lugar (só a aceitação o faz). Os pontos indicados são geocodificados e as coordenadas ficam na
    /// participação, para a rota não voltar a pedi-las (A8, DC-34). Depois de gravar, cria a notificação "Pedido
    /// recebido" para o condutor (com push, RF-F006-13) e envia o evento em tempo real (RF-F006-15).
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="passageiroId">Utilizador que faz o pedido.</param>
    /// <param name="recolha">Ponto de recolha; <c>null</c> segue a origem da viagem (P-20).</param>
    /// <param name="destino">Ponto de destino; <c>null</c> segue o destino da viagem (P-20).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>O pedido criado, ou o erro que explica a recusa.</returns>
    Task<Resultado<PedidoCriado>> CriarPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct);

    /// <summary>Simula o percurso da viagem se o condutor aceitar um pedido pendente (RF-F004-09, RF-F008-06).</summary>
    /// <remarks>
    /// Não altera nada. Compara a rota guardada com a rota que resultaria de juntar o pedido, calculada pelo mesmo
    /// algoritmo (<see cref="Rotas.IRotaService"/>, P-07), por isso a simulação coincide com a rota depois de aceitar.
    /// Pontos omitidos assumem a origem ou o destino da viagem (P-20). Todo o cálculo tem um limite de 10 segundos.
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="pedidoId">Pedido pendente a simular.</param>
    /// <param name="utilizadorId">Utilizador autenticado; tem de ser o condutor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A simulação, ou o erro que explica a recusa.</returns>
    Task<Resultado<SimulacaoPedido>> SimularAsync(Guid viagemId, Guid pedidoId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Solicita a aceitação de uma participação.</summary>
    /// <remarks>
    /// Só o condutor, só pedidos pendentes de viagens ativas (RF-F004-10, mesmo depois da hora de partida), e só com
    /// lugares livres (RF-F004-11). Ocupa um lugar e regista <c>AceiteEm</c>.
    /// </remarks>
    Task<Resultado> AceitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
    /// <summary>Solicita a rejeição de uma participação pendente.</summary>
    /// <remarks>Só o condutor (RF-F004-12).</remarks>
    Task<Resultado> RejeitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
    /// <summary>Solicita o cancelamento de uma participação.</summary>
    /// <remarks>
    /// Só o passageiro (AD-001, F1). Pendente ou aceite (RF-F004-15, RF-F004-16); a aceite só até à hora de partida
    /// (P-03) e liberta o lugar (RF-F004-17).
    /// </remarks>
    Task<Resultado> CancelarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
}
