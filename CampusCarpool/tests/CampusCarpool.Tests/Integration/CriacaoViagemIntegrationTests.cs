
using System.Net;
using System.Text;
using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class CriacaoViagemIntegrationTests
{
    private sealed class MapasHandlerFalso : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct)
        {
            string resposta;

            if (request.RequestUri!.AbsolutePath.Contains(
                "geocode", StringComparison.OrdinalIgnoreCase))
            {
                var url = Uri.UnescapeDataString(
                    request.RequestUri.ToString());

                resposta = url.Contains(
                    "Porto", StringComparison.OrdinalIgnoreCase)
                    ? """
                      {"features":[{"geometry":{"coordinates":[-8.61,41.15]}}]}
                      """
                    : """
                      {"features":[{"geometry":{"coordinates":[-8.43,41.55]}}]}
                      """;
            }
            else
            {
                resposta = """
                    {"features":[{"properties":{"summary":{"distance":55000,"duration":3600}}}]}
                    """;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    resposta,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }

    [Fact]
    public async Task IT008_CriarViagemValida_Devolve201EPersiste()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var condutor = new Utilizador
        {
            Nome = "Condutor IT008",
            Email = "condutor@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(condutor);
        await db.SaveChangesAsync();

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = "chave-falsa-exclusiva-testes"
            })
            .Build();

        using var http = new HttpClient(new MapasHandlerFalso())
{
    BaseAddress = new Uri("https://api.heigit.org/")
};

        var mapas = new MapasService(http, configuracao);

        var controller = new ViagensController(db, mapas);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                condutor.Id.ToString())
                        },
                        "Teste"))
            }
        };

        var partida = DateTime.UtcNow.AddDays(2);

        var resultado = await controller.Criar(
            new ViagensController.CriarViagem(
                "Porto",
                "Braga",
                partida,
                3),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado);

        var viagem = await db.Viagens.SingleAsync();

        Assert.Equal(condutor.Id, viagem.CondutorId);
        Assert.Equal("Porto", viagem.Origem);
        Assert.Equal("Braga", viagem.Destino);

        Assert.Equal(3, viagem.LugaresOferecidos);
        Assert.Equal(3, viagem.LugaresDisponiveis);

        Assert.Equal(55m, viagem.DistanciaKm);
        Assert.Equal(60, viagem.DuracaoMinutos);

        Assert.Equal("Ativa", viagem.Estado);
        Assert.True(viagem.PartidaEm > DateTime.UtcNow);
    }
}
