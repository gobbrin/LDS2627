using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

public sealed class PedidosUserIdProvider : IUserIdProvider
{
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
