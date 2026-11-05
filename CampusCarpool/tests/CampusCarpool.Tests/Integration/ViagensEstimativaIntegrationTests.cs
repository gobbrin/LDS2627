using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Estimativa de custo nos detalhes da viagem: distância guardada sem passageiros, rota recalculada com passageiros e ausência de estimativa sem distância.</summary>
/// <remarks>
/// SQLite em memória; OpenRouteService simulado por <c>HttpMessageHandler</c>.
/// </remarks>
public sealed class ViagensEstimativaIntegrationTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            _connection.Open();
            Db = new CampusCarpoolDbContext(
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(_connection).Options);
            Db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }

        public async Task<Viagem> CriarViagemAsync(decimal distanciaKm, bool rotaGuardada = true)
        {
            var condutor = new Utilizador
            {
                Nome = "Condutor",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };
            Db.Utilizadores.Add(condutor);

            var viagem = new Viagem
            {
                CondutorId = condutor.Id,
                Origem = "Porto",
                Destino = "Braga",
                OrigemLatitude = 41.15m,
                OrigemLongitude = -8.61m,
                DestinoLatitude = 41.55m,
                DestinoLongitude = -8.42m,
                PartidaEm = DateTime.UtcNow.AddDays(3),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                DistanciaKm = distanciaKm,
                Estado = "Ativa"
            };

            if (rotaGuardada && distanciaKm > 0)
            {
                RotaViagemService.GuardarDireta(viagem, new ResultadoRota(
                    distanciaKm,
                    30,
                    new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude),
                    new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude)));
            }

            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();
            return viagem;
        }
    }

    private static ViagensController CriarController(
        CampusCarpoolDbContext db, MapasFalsos handler)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = "chave-ficticia-apenas-para-testes",
                ["Custos:PrecoPorKm"] = "0.10"
            })
            .Build();

        var cliente = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openrouteservice.org/")
        };
        var mapas = new MapasService(cliente, configuracao);
        var custos = new EstimativaCustoService(configuracao);
        return ComUtilizador(new ViagensController(db, mapas, custos, new RotaViagemService(mapas)));
    }

    // A viagem da fixture é pesquisável, por isso qualquer utilizador a consulta.
    private static ViagensController ComUtilizador(ViagensController controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Teste"))
            }
        };
        return controller;
    }

    private static T Campo<T>(object resposta, string nome)
    {
        var propriedade = resposta.GetType().GetProperty(nome);
        Assert.NotNull(propriedade);
        return (T)propriedade.GetValue(resposta)!;
    }

    [Fact]
    public async Task SemPassageiros_UsaDistanciaGuardadaECalculaTresEuros()
    {
        using var f = new Fixture();
        var viagem = await f.CriarViagemAsync(30m);
        var handler = new MapasFalsos { MetrosPorSegmento = 30000, SegundosPorSegmento = 1800 };
        var controller = CriarController(f.Db, handler);

        var resposta = Assert.IsType<OkObjectResult>(
            await controller.Consultar(viagem.Id, default));
        var estimativa = Campo<ResultadoEstimativaCusto>(resposta.Value!, "EstimativaCusto");

        Assert.Equal(3m, estimativa.CustoPorParticipante);
        Assert.Equal(1, estimativa.TotalParticipantes);
        Assert.True(estimativa.Indicativo);
        Assert.Equal(0, handler.ChamadasRotas);
    }

    [Fact]
    public async Task DoisPassageirosAceites_UtilizaRotaCompletaEDividePorTres()
    {
        using var f = new Fixture();
        // Rota por recalcular, com uma distância antiga diferente da rota simulada de 30 km.
        var viagem = await f.CriarViagemAsync(10m, rotaGuardada: false);
        var passageiros = new List<Participacao>();

        for (var i = 0; i < 2; i++)
        {
            var utilizador = new Utilizador
            {
                Nome = $"Passageiro {i + 1}",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };
            f.Db.Utilizadores.Add(utilizador);
            passageiros.Add(new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = utilizador.Id,
                Estado = EstadoParticipacao.Aceite,
                Recolha = null,
                Destino = null
            });
        }

        f.Db.Participacoes.AddRange(passageiros);
        await f.Db.SaveChangesAsync();
        var handler = new MapasFalsos { MetrosPorSegmento = 30000, SegundosPorSegmento = 1800 };
        var controller = CriarController(f.Db, handler);

        var resposta = Assert.IsType<OkObjectResult>(
            await controller.Consultar(viagem.Id, default));
        var estimativa = Campo<ResultadoEstimativaCusto>(resposta.Value!, "EstimativaCusto");

        Assert.Equal(30m, estimativa.DistanciaKm);
        Assert.Equal(1m, estimativa.CustoPorParticipante);
        Assert.Equal(3, estimativa.TotalParticipantes);
        Assert.True(handler.ChamadasRotas > 0);
        Assert.DoesNotContain("PontosOrdenados", resposta.Value!.GetType().GetProperties()
            .Select(p => p.Name));
        Assert.DoesNotContain("Segmentos", resposta.Value!.GetType().GetProperties()
            .Select(p => p.Name));
    }

    [Fact]
    public async Task DistanciaEmFalta_NaoInventaEstimativa()
    {
        using var f = new Fixture();
        var viagem = await f.CriarViagemAsync(0m);
        var handler = new MapasFalsos { MetrosPorSegmento = 30000, SegundosPorSegmento = 1800 };
        var controller = ComUtilizador(new ViagensController(
            f.Db,
            new MapasService(new HttpClient(), new ConfigurationBuilder().Build()),
            new EstimativaCustoService(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Custos:PrecoPorKm"] = "0.10"
                }).Build())));

        var resposta = Assert.IsType<OkObjectResult>(
            await controller.Consultar(viagem.Id, default));

        Assert.Null(resposta.Value!.GetType().GetProperty("EstimativaCusto")!.GetValue(resposta.Value));
        Assert.False(string.IsNullOrWhiteSpace(
            Campo<string>(resposta.Value, "EstimativaIndisponivelMotivo")));
    }
}
