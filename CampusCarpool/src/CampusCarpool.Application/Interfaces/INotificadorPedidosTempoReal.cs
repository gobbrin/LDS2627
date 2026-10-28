namespace CampusCarpool.Application.Interfaces;

// Publica eventos de pedidos para os utilizadores ligados
// à aplicação através do canal de comunicação em tempo real.
public interface INotificadorPedidosTempoReal
{
    Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);

    Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);

    Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct);
}
