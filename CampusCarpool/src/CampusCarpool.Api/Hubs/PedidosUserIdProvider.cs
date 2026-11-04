using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

/// <summary>Diz ao SignalR qual é o utilizador de cada ligação: o id da claim <c>sub</c> do JWT, em formato "D".</summary>
/// <remarks>O <c>NotificadorPedidosSignalR</c> usa o mesmo formato em <c>Clients.User(id.ToString("D"))</c>.</remarks>
public sealed class PedidosUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection)
    {
        var valor =
            connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue("sub");

        return Guid.TryParse(valor, out var utilizadorId)
            ? utilizadorId.ToString("D")
            : null;
    }
}
