using CampusCarpool.Api.Hubs;
using CampusCarpool.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public class NotificadorPedidosSignalRTests
{
    [Fact]
    public async Task PedidoRecebidoAsync_DeveEnviarEventoAoDestinatario()
    {
        await VerificarEventoAsync(
            (notificador, destinatarioId, pedidoId) =>
                notificador.PedidoRecebidoAsync(destinatarioId, pedidoId, CancellationToken.None),
            "PedidoRecebido");
    }

    [Fact]
    public async Task PedidoAceiteAsync_DeveEnviarEventoAoDestinatario()
    {
        await VerificarEventoAsync(
            (notificador, destinatarioId, pedidoId) =>
                notificador.PedidoAceiteAsync(destinatarioId, pedidoId, CancellationToken.None),
            "PedidoAceite");
    }

    [Fact]
    public async Task PedidoRejeitadoAsync_DeveEnviarEventoAoDestinatario()
    {
        await VerificarEventoAsync(
            (notificador, destinatarioId, pedidoId) =>
                notificador.PedidoRejeitadoAsync(destinatarioId, pedidoId, CancellationToken.None),
            "PedidoRejeitado");
    }

    [Fact]
    public async Task PedidoRecebidoAsync_QuandoEnvioFalha_NaoDevePropagarExcecao()
    {
        var hubContexto = new HubContextoFalso();
        hubContexto.Clientes.Proxy.FalharEnvio = true;

        var notificador = new NotificadorPedidosSignalR(hubContexto, NullLogger<NotificadorPedidosSignalR>.Instance);

        var destinatarioId = Guid.NewGuid();
        var pedidoId = Guid.NewGuid();

        var excecao = await Record.ExceptionAsync(() =>
            notificador.PedidoRecebidoAsync(
                destinatarioId,
                pedidoId,
                CancellationToken.None));

        Assert.Null(excecao);
    }

    private static async Task VerificarEventoAsync(
        Func<NotificadorPedidosSignalR, Guid, Guid, Task> acao,
        string eventoEsperado)
    {
        var hubContexto = new HubContextoFalso();
        var notificador = new NotificadorPedidosSignalR(hubContexto, NullLogger<NotificadorPedidosSignalR>.Instance);

        var destinatarioId = Guid.NewGuid();
        var pedidoId = Guid.NewGuid();

        await acao(notificador, destinatarioId, pedidoId);

        Assert.Equal(
            destinatarioId.ToString("D"),
            hubContexto.Clientes.DestinatarioId);

        Assert.Equal(
            eventoEsperado,
            hubContexto.Clientes.Proxy.NomeEvento);

        var argumentos = hubContexto.Clientes.Proxy.Argumentos;

        Assert.NotNull(argumentos);
        Assert.Single(argumentos);

        var payload = argumentos[0];
        Assert.NotNull(payload);

        var propriedade = payload.GetType().GetProperty("PedidoId");

        Assert.NotNull(propriedade);
        Assert.Equal(pedidoId, propriedade.GetValue(payload));
    }

    private sealed class HubContextoFalso : IHubContext<PedidosHub>
    {
        public ClientesFalsos Clientes { get; } = new();

        public IHubClients Clients => Clientes;

        public IGroupManager Groups => new GestorGruposFalso();
    }

    private sealed class ClientesFalsos : IHubClients
    {
        public string? DestinatarioId { get; private set; }

        public ClienteFalso Proxy { get; } = new();

        public IClientProxy All => Proxy;

        public IClientProxy AllExcept(
            IReadOnlyList<string> excludedConnectionIds) => Proxy;

        public IClientProxy Client(string connectionId) => Proxy;

        public IClientProxy Clients(
            IReadOnlyList<string> connectionIds) => Proxy;

        public IClientProxy Group(string groupName) => Proxy;

        public IClientProxy GroupExcept(
            string groupName,
            IReadOnlyList<string> excludedConnectionIds) => Proxy;

        public IClientProxy Groups(
            IReadOnlyList<string> groupNames) => Proxy;

        public IClientProxy User(string userId)
        {
            DestinatarioId = userId;
            return Proxy;
        }

        public IClientProxy Users(
            IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class ClienteFalso : IClientProxy
    {
        public string? NomeEvento { get; private set; }

        public object?[]? Argumentos { get; private set; }

        public bool FalharEnvio { get; set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            if (FalharEnvio)
            {
                return Task.FromException(
                    new InvalidOperationException("Falha simulada no envio SignalR."));
            }

            NomeEvento = method;
            Argumentos = args;

            return Task.CompletedTask;
        }
    }

    private sealed class GestorGruposFalso : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

}