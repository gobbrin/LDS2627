using CampusCarpool.Api.Hubs;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Api.Services;

/// <summary>Implementação SignalR de <see cref="INotificadorPedidosTempoReal"/>: envia eventos pelo <see cref="PedidosHub"/>.</summary>
/// <remarks>
/// Cada evento vai para todas as ligações do utilizador (<c>Clients.User</c>, identificado pelo
/// <see cref="PedidosUserIdProvider"/>), com o nome <c>PedidoRecebido</c>, <c>PedidoAceite</c> ou
/// <c>PedidoRejeitado</c> e o corpo <c>{ pedidoId }</c>. O cliente vai buscar os detalhes por HTTP.
/// Uma falha de envio só fica no log: a operação que a originou já está gravada e não é desfeita.
/// </remarks>
public sealed class NotificadorPedidosSignalR(
    IHubContext<PedidosHub> hubContext,
    ILogger<NotificadorPedidosSignalR> logger) : INotificadorPedidosTempoReal
{
    /// <inheritdoc />
    public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return EnviarEventoAsync(
            destinatarioId,
            pedidoId,
            "PedidoRecebido",
            ct);
    }

    /// <inheritdoc />
    public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct)
    {
        return EnviarEventoAsync(
            destinatarioId,
            pedidoId,
            "PedidoAceite",
            ct);
    }

    /// <inheritdoc />
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
