
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class DispositivosHttpIntegrationTests
{
    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(
            options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.Authorization.ToString() != "Bearer teste")
                return Task.FromResult(AuthenticateResult.NoResult());

            var id = Request.Headers["X-Test-User-Id"].ToString();

            if (!Guid.TryParse(id, out var utilizadorId))
                return Task.FromResult(
                    AuthenticateResult.Fail("Identificador invalido"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, utilizadorId.ToString())
            };

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, Scheme.Name));

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection =
            new("Data Source=:memory:");

        public ApiFactory()
        {
            connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=(local);Database=CampusCarpool_Tests;");

            builder.UseSetting(
                "Jwt:Key",
                "CHAVE_DE_TESTES_I054_COM_MAIS_DE_32_BYTES_12345");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CampusCarpoolDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CampusCarpoolDbContext>>();

                services.AddDbContext<CampusCarpoolDbContext>(
                    options => options.UseSqlite(connection));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", _ => { });

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();

                scope.ServiceProvider
                    .GetRequiredService<CampusCarpoolDbContext>()
                    .Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                connection.Dispose();
        }
    }

    private static void Autenticar(HttpClient client, Guid utilizadorId)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "teste");

        client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id", utilizadorId.ToString());
    }

    private static async Task<Guid> CriarUtilizadorAsync(
        ApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var utilizador = new Utilizador
        {
            Nome = "Utilizador Teste",
            Email = email,
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        return utilizador.Id;
    }

    [Fact]
    public async Task I047_RegistarToken_Devolve204EPersisteAtivo()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var utilizadorId = await CriarUtilizadorAsync(
            factory, "registo@estg.ipp.pt");

        Autenticar(client, utilizadorId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-registo" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var dispositivo = await db.Dispositivos.SingleAsync();

        Assert.Equal(utilizadorId, dispositivo.UtilizadorId);
        Assert.Equal("token-fcm-registo", dispositivo.Token);
        Assert.True(dispositivo.Ativo);
    }

    [Fact]
    public async Task I047_RegistarMesmoToken_NaoCriaDuplicados()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var utilizadorId = await CriarUtilizadorAsync(
            factory, "duplicado@estg.ipp.pt");

        Autenticar(client, utilizadorId);

        var primeira = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-duplicado" });

        var segunda = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-duplicado" });

        Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Equal(1, await db.Dispositivos.CountAsync());
        Assert.True((await db.Dispositivos.SingleAsync()).Ativo);
    }

    [Fact]
    public async Task I047_RemoverToken_Devolve204EDesativa()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var utilizadorId = await CriarUtilizadorAsync(
            factory, "remover@estg.ipp.pt");

        Autenticar(client, utilizadorId);

        var registo = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-remover" });

        Assert.Equal(HttpStatusCode.NoContent, registo.StatusCode);

        using var pedido = new HttpRequestMessage(
            HttpMethod.Delete, "/api/v1/dispositivos")
        {
            Content = JsonContent.Create(
                new { Token = "token-fcm-remover" })
        };

        var response = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var dispositivo = await db.Dispositivos.SingleAsync();
        Assert.False(dispositivo.Ativo);
    }

    [Fact]
    public async Task I047_OutroUtilizador_NaoPodeDesativarToken()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var primeiroId = await CriarUtilizadorAsync(
            factory, "primeiro@estg.ipp.pt");

        var segundoId = await CriarUtilizadorAsync(
            factory, "segundo@estg.ipp.pt");

        Autenticar(client, primeiroId);

        var registo = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-protegido" });

        Assert.Equal(HttpStatusCode.NoContent, registo.StatusCode);

        Autenticar(client, segundoId);

        using var pedido = new HttpRequestMessage(
            HttpMethod.Delete, "/api/v1/dispositivos")
        {
            Content = JsonContent.Create(
                new { Token = "token-fcm-protegido" })
        };

        var response = await client.SendAsync(pedido);

        // A operação é idempotente, mas não deve alterar
        // o dispositivo pertencente ao primeiro utilizador.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var dispositivo = await db.Dispositivos.SingleAsync();

        Assert.Equal(primeiroId, dispositivo.UtilizadorId);
        Assert.True(dispositivo.Ativo);
    }

    [Fact]
    public async Task I047_TokenInvalido_Devolve400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var utilizadorId = await CriarUtilizadorAsync(
            factory, "invalido@estg.ipp.pt");

        Autenticar(client, utilizadorId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Equal(0, await db.Dispositivos.CountAsync());
    }

    [Fact]
    public async Task I047_SemAutenticacao_Devolve401()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/dispositivos",
            new { Token = "token-fcm-sem-autenticacao" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Equal(0, await db.Dispositivos.CountAsync());
    }
}
