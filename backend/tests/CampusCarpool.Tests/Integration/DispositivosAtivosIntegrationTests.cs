using CampusCarpool.Application.Abstractions.Integrations;

using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>IT-039: um dispositivo desativado deixa de aparecer nos ativos e não recebe push.</summary>
/// <remarks>
/// SQLite em memória; o FCM é simulado por um <c>HttpMessageHandler</c> que regista os pedidos.
/// </remarks>
public sealed class DispositivosAtivosIntegrationTests
{
    [Fact]
    public async Task IT039_TokenDesativado_NaoApareceNosDispositivosAtivos()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseBaseDeTeste(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.CriarEsquemaDeTesteAsync();

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
            .UseBaseDeTeste(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.CriarEsquemaDeTesteAsync();

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
            NullLogger<ServicoPush>.Instance,
            new CacheTokenFcm());

        await push.EnviarAsync(new NotificacaoPushPendente(Guid.NewGuid(), utilizador.Id, null, "Pedido aceite", "O teu pedido foi aceite.", "PedidoAceite"), CancellationToken.None);

        Assert.Equal(0, pedidosHttp);
    }

    /// <summary>
    /// R5 (teste em falta da R4): um token que o FCM recusa (UNREGISTERED) fica inativo no repositório real, pela conta que for;
    /// os outros dispositivos do mesmo utilizador continuam ativos, e um token desconhecido não dá erro.
    /// </summary>
    [Fact]
    public async Task I112_89_TokenInvalido_DesativaSoEsseDispositivo()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseBaseDeTeste(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.CriarEsquemaDeTesteAsync();

        var utilizador = new Utilizador
        {
            Nome = "Passageiro teste",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var repositorio = new RepositorioDispositivo(db);
        await repositorio.RegistarAsync(utilizador.Id, "token-telemovel", CancellationToken.None);
        await repositorio.RegistarAsync(utilizador.Id, "token-tablet", CancellationToken.None);

        // O ServicoPush só conhece o token (não sabe a conta): desativa por token.
        await repositorio.DesativarTokenInvalidoAsync("token-telemovel", CancellationToken.None);
        await repositorio.DesativarTokenInvalidoAsync("token-que-nao-existe", CancellationToken.None);

        var ativos = await repositorio.ListarAtivosAsync(utilizador.Id, CancellationToken.None);
        Assert.Equal("token-tablet", Assert.Single(ativos).Token);

        // O registo fica na BD, só inativo (um novo registo do mesmo token volta a ativá-lo).
        var invalido = await db.Dispositivos.AsNoTracking().SingleAsync(d => d.Token == "token-telemovel");
        Assert.False(invalido.Ativo);
        Assert.Equal(2, await db.Dispositivos.CountAsync());
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
