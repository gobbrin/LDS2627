namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>
/// Publica eventos de pedidos para os utilizadores ligados
/// à aplicação através do canal de comunicação em tempo real.
/// </summary>
/// <remarks>
/// Serve P-05 (interface atualizada em 5 segundos, RF-F006-09 e RF-F006-15). Implementado na Api por
/// <c>NotificadorPedidosSignalR</c>, que envia para o hub <c>/hubs/pedidos</c>. Só deve ser chamado depois de
/// a decisão estar gravada, para não anunciar uma alteração que foi desfeita.
/// </remarks>
public interface INotificadorPedidosTempoReal
{
    /// <summary>Avisa o condutor de que recebeu um pedido de participação.</summary>
    /// <param name="destinatarioId">Condutor.</param>
    /// <param name="pedidoId">Participação criada.</param>
    /// <param name="ct">Cancelamento.</param>
    Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);

    /// <summary>Avisa o passageiro de que o seu pedido foi aceite.</summary>
    Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);

    /// <summary>Avisa o passageiro de que o seu pedido foi rejeitado.</summary>
    Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);
}
