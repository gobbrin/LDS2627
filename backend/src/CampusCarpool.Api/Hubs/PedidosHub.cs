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
/// (DC-46, DEF-BE-055: inevitável no WebSocket; mitigação no manual de instalação). Um token revogado é recusado ao
/// ligar, a ligação fecha quando o token expira e fecha logo no fim de sessão (BE-18), pela
/// <see cref="LigacoesPorSessao"/>.
/// </remarks>
/// <param name="ligacoes">Lista das ligações abertas por sessão (Singleton).</param>
[Authorize]
public sealed class PedidosHub(LigacoesPorSessao ligacoes) : Hub
{
    /// <summary>Guarda a ligação nova com o <c>jti</c> do token, para o fim de sessão a poder fechar.</summary>
    public override Task OnConnectedAsync()
    {
        ligacoes.Registar(Context);
        return base.OnConnectedAsync();
    }

    /// <summary>Esquece a ligação que fechou.</summary>
    /// <param name="exception">Erro que fechou a ligação, se houve.</param>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ligacoes.Remover(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
