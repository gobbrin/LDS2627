
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class HistoricoHttpIntegrationTests
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
            if (!Request.Headers.TryGetValue("Authorization", out var authorization) ||
                authorization.ToString() != "Bearer teste")
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var id = Request.Headers["X-Test-User-Id"].ToString();

            if (!Guid.TryParse(id, out var utilizadorId))
                return Task.FromResult(AuthenticateResult.Fail("ID invalido"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, utilizadorId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

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

services.AddDbContext<CampusCarpoolDbContext>(options =>
    options.UseSqlite(connection));
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

    private static void Autenticar(HttpClient client, Guid id)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "teste");

        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id", id.ToString());
    }

    [Fact]
    public async Task IT033_SemToken_Devolve401()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/historico");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IT033_ComAutenticacao_Devolve200()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        Autenticar(client, Guid.NewGuid());

        var response = await client.GetAsync("/api/v1/historico");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IT033_Isolamento_DevolveApenasDadosDoUtilizador()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var primeiroId = Guid.NewGuid();
        var segundoId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<CampusCarpoolDbContext>();

            db.Utilizadores.AddRange(
                new Utilizador
                {
                    Id = primeiroId,
                    Nome = "Primeiro",
                    Email = "primeiro@estg.ipp.pt"
                },
                new Utilizador
                {
                    Id = segundoId,
                    Nome = "Segundo",
                    Email = "segundo@estg.ipp.pt"
                });

            db.Viagens.AddRange(
                new Viagem
                {
                    CondutorId = primeiroId,
                    Origem = "Porto",
                    Destino = "Braga",
                    Estado = "Concluida",
                    PartidaEm = DateTime.UtcNow.AddDays(-2),
                    LugaresOferecidos = 2,
                    LugaresDisponiveis = 2
                },
                new Viagem
                {
                    CondutorId = segundoId,
                    Origem = "Lisboa",
                    Destino = "Coimbra",
                    Estado = "Concluida",
                    PartidaEm = DateTime.UtcNow.AddDays(-2),
                    LugaresOferecidos = 2,
                    LugaresDisponiveis = 2
                });

            await db.SaveChangesAsync();
        }

        Autenticar(client, primeiroId);

        var response = await client.GetAsync("/api/v1/historico");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Porto", body);
        Assert.DoesNotContain("Lisboa", body);
    }
}
