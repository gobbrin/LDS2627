using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

[Authorize]
public sealed class PedidosHub : Hub
{
}
