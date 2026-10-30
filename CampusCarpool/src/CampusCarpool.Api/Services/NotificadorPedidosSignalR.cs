using CampusCarpool.Api.Hubs;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Api.Services;

public sealed class NotificadorPedidosSignalR(
    IHubContext<PedidosHub> hubContext,
    ILogger<NotificadorPedidosSignalR> logger) : INotificadorPedidosTempoReal
{
    public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return EnviarEventoAsync(
            destinatarioId,
            pedidoId,
            "PedidoRecebido",
            ct);
    }

    public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return EnviarEventoAsync(
            destinatarioId,
            pedidoId,
            "PedidoAceite",
            ct);
    }

    public Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return EnviarEventoAsync(
            destinatarioId,
            pedidoId,
            "PedidoRejeitado",
            ct);
    }

    private async Task EnviarEventoAsync(Guid destinatarioId, Guid pedidoId, string nomeEvento, CancellationToken ct)
    {
        try
        {
            await hubContext.Clients
                .User(destinatarioId.ToString("D"))
                .SendAsync(
                    nomeEvento,
                    new { PedidoId = pedidoId },
                    ct);
        }
        catch (Exception ex)
        {
            AvisoFalhaEnvioSignalR(logger, nomeEvento, pedidoId, destinatarioId, ex);
        }
    }

    private static readonly Action<
        ILogger,
        string,
        Guid,
        Guid,
        Exception?> AvisoFalhaEnvioSignalR =
        LoggerMessage.Define<string, Guid, Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(AvisoFalhaEnvioSignalR)),
            "Não foi possível enviar o evento SignalR {NomeEvento} para o pedido {PedidoId} e o destinatário {DestinatarioId}.");
}
