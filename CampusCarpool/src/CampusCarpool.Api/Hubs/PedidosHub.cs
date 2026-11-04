using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

/// <summary>
/// Hub SignalR <c>/hubs/pedidos</c>: canal em tempo real dos eventos de pedidos (P-05, RF-F006-09, RF-F006-15).
/// </summary>
/// <remarks>
/// Não tem métodos: o cliente só recebe eventos, enviados pelo <c>NotificadorPedidosSignalR</c>. Exige
/// autenticação; como o WebSocket não envia cabeçalhos, o JWT vai na query string <c>?access_token=</c>, aceite
/// só neste caminho (<c>OnMessageReceived</c> no <c>Program.cs</c>). Esse token pode ficar nos logs de acesso
/// (DC-46). Um token revogado é recusado ao ligar, mas uma ligação já aberta não é fechada no fim de sessão.
/// </remarks>
[Authorize]
public sealed class PedidosHub : Hub
{
}
