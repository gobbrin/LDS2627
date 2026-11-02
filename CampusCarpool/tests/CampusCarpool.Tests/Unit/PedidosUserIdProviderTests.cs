using System.Security.Claims;
using CampusCarpool.Api.Hubs;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public class PedidosUserIdProviderTests
{
    [Fact]
    public void GetUserId_DeveDevolverIdQuandoExisteNameIdentifierValido()
    {
        var utilizadorId = Guid.NewGuid();
        var provider = new PedidosUserIdProvider();

        var connection = CriarContexto(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            utilizadorId.ToString())
                    ],
                    "Test")));

        var resultado = provider.GetUserId(connection);

        Assert.Equal(utilizadorId.ToString("D"), resultado);
    }

    [Fact]
    public void GetUserId_DeveUsarSubQuandoNameIdentifierNaoExiste()
    {
        var utilizadorId = Guid.NewGuid();
        var provider = new PedidosUserIdProvider();

        var connection = CriarContexto(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim("sub", utilizadorId.ToString())
                    ],
                    "Test")));

        var resultado = provider.GetUserId(connection);

        Assert.Equal(utilizadorId.ToString("D"), resultado);
    }

    [Fact]
    public void GetUserId_DeveDevolverNullQuandoIdEInvalido()
    {
        var provider = new PedidosUserIdProvider();

        var connection = CriarContexto(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            "id-invalido")
                    ],
                    "Test")));

        var resultado = provider.GetUserId(connection);

        Assert.Null(resultado);
    }

    private static HubConnectionContext CriarContexto(ClaimsPrincipal utilizador)
    {
        var connectionContext = new DefaultConnectionContext(Guid.NewGuid().ToString());

        connectionContext.User = utilizador;

        return new HubConnectionContext(
            connectionContext,
            new HubConnectionContextOptions(),
            NullLoggerFactory.Instance);
    }

}
