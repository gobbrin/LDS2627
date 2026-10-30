
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class DispositivosAtivosIntegrationTests
{
    [Fact]
    public async Task IT039_TokenDesativado_NaoApareceNosDispositivosAtivos()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Passageiro teste",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var repositorio = new RepositorioDispositivo(db);

        await repositorio.RegistarAsync(
            utilizador.Id,
            "token-dispositivo-teste",
            CancellationToken.None);

        var antes = await repositorio.ListarAtivosAsync(
            utilizador.Id,
            CancellationToken.None);

        Assert.Single(antes);

        await repositorio.DesativarAsync(
            utilizador.Id,
            "token-dispositivo-teste",
            CancellationToken.None);

        var depois = await repositorio.ListarAtivosAsync(
            utilizador.Id,
            CancellationToken.None);

        Assert.Empty(depois);

        var dispositivo = await db.Dispositivos.SingleAsync();

        Assert.False(dispositivo.Ativo);
    }

    [Fact]
public async Task IT039_TokenDesativado_NaoOriginaEnvioPush()
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new CampusCarpoolDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var utilizador = new Utilizador
    {
        Nome = "Passageiro teste",
        Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
        EmailConfirmado = true
    };

    db.Utilizadores.Add(utilizador);
    await db.SaveChangesAsync();

    var repositorio = new RepositorioDispositivo(db);

    const string token = "token-fcm-desativado";

    await repositorio.RegistarAsync(
        utilizador.Id, token, CancellationToken.None);

    await repositorio.DesativarAsync(
        utilizador.Id, token, CancellationToken.None);

    var pedidosHttp = 0;

    using var http = new HttpClient(
        new HandlerHttpFalso(() => pedidosHttp++));

    var configuracao = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>())
        .Build();

    var push = new ServicoPush(
        http,
        configuracao,
        repositorio,
        NullLogger<ServicoPush>.Instance);

    await push.EnviarAsync(
        utilizador.Id,
        "Pedido aceite",
        "O teu pedido foi aceite.",
        "PedidoAceite",
        CancellationToken.None);

    Assert.Equal(0, pedidosHttp);
}

private sealed class HandlerHttpFalso(Action registarPedido)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        registarPedido();

        return Task.FromResult(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
}
