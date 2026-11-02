using System.Net;
using System.Security.Claims;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class PedidosHubIntegrationTests
{
    private static readonly Guid UtilizadorA =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid UtilizadorB =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task LigacaoSemAutenticacao_DeveSerRejeitada()
    {
        await using var app = await CriarAplicacaoAsync();
        var hubUrl = ObterHubUrl(app);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task PedidoRecebido_DeveSerEntregueApenasAoDestinatario()
    {
        await using var app = await CriarAplicacaoAsync();
        var hubUrl = ObterHubUrl(app);

        var notificacaoA = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        var notificacaoB = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var ligacaoA = CriarLigacao(hubUrl, "utilizador-a");
        await using var ligacaoB = CriarLigacao(hubUrl, "utilizador-b");

        ligacaoA.On<JsonPedido>(
            "PedidoRecebido",
            pedido => notificacaoA.TrySetResult(pedido.PedidoId));

        ligacaoB.On<JsonPedido>(
            "PedidoRecebido",
            pedido => notificacaoB.TrySetResult(pedido.PedidoId));

        await ligacaoA.StartAsync();
        await ligacaoB.StartAsync();

        var notificador = app.Services.GetRequiredService<INotificadorPedidosTempoReal>();

        var pedidoId = Guid.NewGuid();

        await notificador.PedidoRecebidoAsync(
            UtilizadorA,
            pedidoId,
            CancellationToken.None);

        var pedidoRecebido = await notificacaoA.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        Assert.Equal(pedidoId, pedidoRecebido);

        // Dá tempo para detetar uma eventual entrega indevida a B.
        await Task.Delay(300);

        Assert.False(notificacaoB.Task.IsCompleted);
    }

    [Theory]
    [InlineData("PedidoAceite")]
    [InlineData("PedidoRejeitado")]
    public async Task DecisaoPedido_DeveSerEntregueApenasAoDestinatario(string nomeEvento)
    {
        await using var app = await CriarAplicacaoAsync();

        var hubUrl = ObterHubUrl(app);

        var notificacaoA = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        var notificacaoB = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var ligacaoA = CriarLigacao(hubUrl, "utilizador-a");
        await using var ligacaoB = CriarLigacao(hubUrl, "utilizador-b");

        ligacaoA.On<JsonPedido>(
            nomeEvento,
            pedido => notificacaoA.TrySetResult(pedido.PedidoId));

        ligacaoB.On<JsonPedido>(
            nomeEvento,
            pedido => notificacaoB.TrySetResult(pedido.PedidoId));

        await ligacaoA.StartAsync();
        await ligacaoB.StartAsync();

        var pedidoId = Guid.NewGuid();

        var notificador = app.Services.GetRequiredService<INotificadorPedidosTempoReal>();

        if (nomeEvento == "PedidoAceite")
        {
            await notificador.PedidoAceiteAsync(
                UtilizadorA,
                pedidoId,
                CancellationToken.None);
        }
        else
        {
            await notificador.PedidoRejeitadoAsync(
                UtilizadorA,
                pedidoId,
                CancellationToken.None);
        }

        var pedidoRecebido = await notificacaoA.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(pedidoId, pedidoRecebido);

        await Task.Delay(300);

        Assert.False(notificacaoB.Task.IsCompleted);
    }

    private static async Task<WebApplication> CriarAplicacaoAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        builder.Services
            .AddAuthentication("Teste")
            .AddScheme<AuthenticationSchemeOptions, AutenticacaoTesteHandler>(
                "Teste", _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton<
            Microsoft.AspNetCore.SignalR.IUserIdProvider,
            PedidosUserIdProvider>();

        builder.Services.AddSingleton<
            INotificadorPedidosTempoReal,
            NotificadorPedidosSignalR>();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<PedidosHub>("/hubs/pedidos");

        await app.StartAsync();

        return app;
    }

    private static string ObterHubUrl(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        var address = Assert.Single(addresses!.Addresses);

        return $"{address}/hubs/pedidos";
    }

    private static HubConnection CriarLigacao(
        string hubUrl,
        string token)
    {
        return new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers["Authorization"] = $"Bearer {token}";
            })
            .Build();
    }

    private sealed record JsonPedido(Guid PedidoId);

    private sealed class AutenticacaoTesteHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AutenticacaoTesteHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult>
            HandleAuthenticateAsync()
        {
            var token = Request.Headers.Authorization.ToString();

            var utilizadorId = token switch
            {
                "Bearer utilizador-a" => UtilizadorA,
                "Bearer utilizador-b" => UtilizadorB,
                _ => Guid.Empty
            };

            if (utilizadorId == Guid.Empty)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    utilizadorId.ToString("D"))
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
