using CampusCarpool.Api.Controllers;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

/// <summary>Diz ao SignalR qual é o utilizador de cada ligação: o id da claim <c>sub</c> do JWT, em formato "D".</summary>
/// <remarks>
/// O <c>NotificadorPedidosSignalR</c> usa o mesmo formato em <c>Clients.User(id.ToString("D"))</c>. O id lê-se como
/// nos controllers, com <see cref="UtilizadorAutenticado.IdUtilizador"/> (BE-31).
/// </remarks>
public sealed class PedidosUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection)
    {
        Guid? utilizadorId = connection.User?.IdUtilizador();

        return utilizadorId?.ToString("D");
    }
}
