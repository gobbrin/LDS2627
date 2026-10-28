
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
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

public sealed class AvaliacoesHttpIntegrationTests
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

    private static async Task<(Guid viagemId, Guid avaliadorId, Guid avaliadoId)>
        PrepararViagemAsync(ApiFactory factory, bool participacaoAceite = true)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var condutor = new Utilizador
        {
            Nome = "Condutor",
            Email = "condutor.avaliacoes@estg.ipp.pt",
            EmailConfirmado = true
        };

        var passageiro = new Utilizador
        {
            Nome = "Passageiro",
            Email = "passageiro.avaliacoes@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.AddRange(condutor, passageiro);

        var viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(-2),
            Estado = "Concluida",
            LugaresOferecidos = 3,
            LugaresDisponiveis = 2
        };

        db.Viagens.Add(viagem);

        db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = participacaoAceite
                ? EstadoParticipacao.Aceite
                : EstadoParticipacao.Pendente
        });

        await db.SaveChangesAsync();

        return (viagem.Id, passageiro.Id, condutor.Id);
    }

    [Fact]
    public async Task IT034_AvaliacaoValida_Devolve201EPersiste()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var dados = await PrepararViagemAsync(factory);
        Autenticar(client, dados.avaliadorId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes",
            new
            {
                ViagemId = dados.viagemId,
                AvaliadoId = dados.avaliadoId,
                Classificacao = 5
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        var avaliacao = await db.Avaliacoes.SingleAsync();

        Assert.Equal(dados.viagemId, avaliacao.ViagemId);
        Assert.Equal(dados.avaliadorId, avaliacao.AvaliadorId);
        Assert.Equal(dados.avaliadoId, avaliacao.AvaliadoId);
        Assert.Equal(5, avaliacao.Classificacao);
    }

    [Fact]
    public async Task IT035_AvaliacaoDuplicada_Devolve409()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var dados = await PrepararViagemAsync(factory);
        Autenticar(client, dados.avaliadorId);

        var pedido = new
        {
            ViagemId = dados.viagemId,
            AvaliadoId = dados.avaliadoId,
            Classificacao = 4
        };

        var primeira = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes", pedido);

        var segunda = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes", pedido);

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Equal(1, await db.Avaliacoes.CountAsync());
    }

    [Fact]
    public async Task IT035_ParticipacaoNaoAceite_Devolve409()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var dados = await PrepararViagemAsync(
            factory, participacaoAceite: false);

        Autenticar(client, dados.avaliadorId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes",
            new
            {
                ViagemId = dados.viagemId,
                AvaliadoId = dados.avaliadoId,
                Classificacao = 5
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Empty(await db.Avaliacoes.ToListAsync());
    }

    [Fact]
    public async Task IT035_ClassificacaoInvalida_Devolve400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var dados = await PrepararViagemAsync(factory);
        Autenticar(client, dados.avaliadorId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes",
            new
            {
                ViagemId = dados.viagemId,
                AvaliadoId = dados.avaliadoId,
                Classificacao = 6
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<CampusCarpoolDbContext>();

        Assert.Equal(0, await db.Avaliacoes.CountAsync());
    }

    [Fact]
    public async Task IT035_SemAutenticacao_Devolve401()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/avaliacoes",
            new
            {
                ViagemId = Guid.NewGuid(),
                AvaliadoId = Guid.NewGuid(),
                Classificacao = 5
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
