using CampusCarpool.Api.Hubs;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Services;

public sealed class NotificadorPedidosSignalR(IHubContext<PedidosHub> hubContext) : INotificadorPedidosTempoReal
{
    public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return hubContext.Clients
            .User(destinatarioId.ToString("D"))
            .SendAsync(
                "PedidoRecebido",
                new { PedidoId = pedidoId },
                ct);
    }

    public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return hubContext.Clients
            .User(destinatarioId.ToString("D"))
            .SendAsync(
                "PedidoAceite",
                new { PedidoId = pedidoId },
                ct);
    }

    public Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return hubContext.Clients
            .User(destinatarioId.ToString("D"))
            .SendAsync(
                "PedidoRejeitado",
                new { PedidoId = pedidoId },
                ct);
    }
}
