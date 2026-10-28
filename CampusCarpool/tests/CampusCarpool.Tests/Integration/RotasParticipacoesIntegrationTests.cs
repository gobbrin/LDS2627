
using System.Net;
using System.Security.Claims;
using System.Text;
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

public sealed class RotasParticipacoesIntegrationTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection =
            new("Data Source=:memory:");

        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            connection.Open();

            var options =
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(connection)
                    .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.EnsureCreated();
        }

        public async Task<Utilizador> CriarUtilizador()
        {
            var utilizador = new Utilizador
            {
                Nome = "Utilizador de teste",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };

            Db.Utilizadores.Add(utilizador);
            await Db.SaveChangesAsync();

            return utilizador;
        }

        public async Task<Viagem> CriarViagem(Guid condutorId)
        {
            var viagem = new Viagem
            {
                CondutorId = condutorId,
                Origem = "Porto",
                Destino = "Braga",
                OrigemLatitude = 41.15m,
                OrigemLongitude = -8.61m,
                DestinoLatitude = 41.55m,
                DestinoLongitude = -8.42m,
                PartidaEm = DateTime.UtcNow.AddDays(3),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            };

            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();

            return viagem;
        }

        public void Dispose()
        {
            Db.Dispose();
            connection.Dispose();
        }
    }

    private sealed class MapasSimulados : HttpMessageHandler
    {
        public int ChamadasRotas { get; private set; }
        public int ChamadasGeocodificacao { get; private set; }

        public bool SimularFalha { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var caminho = request.RequestUri!.AbsolutePath;

            string resposta;

            if (SimularFalha)
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }


            if (caminho.Contains("geocode/search"))
            {
                ChamadasGeocodificacao++;

                resposta = """
                {
                  "features": [
                    {
                      "geometry": {
                        "coordinates": [-8.60, 41.20]
                      }
                    }
                  ]
                }
                """;
            }
            else if (caminho.Contains("directions/driving-car"))
            {
                ChamadasRotas++;

                resposta = """
                {
                  "features": [
                    {
                      "properties": {
                        "summary": {
                          "distance": 1000,
                          "duration": 120
                        }
                      }
                    }
                  ]
                }
                """;
            }
            else
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.NotFound));
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

    private static RotasController CriarController(
        CampusCarpoolDbContext db,
        Guid? utilizadorId,
        MapasSimulados handler)
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
            BaseAddress =
                new Uri("https://api.openrouteservice.org/")
        };

        var mapas = new MapasService(cliente, configuracao);
        var custos = new EstimativaCustoService(configuracao);
        var controller = new RotasController(mapas, custos, db);

        var claims = utilizadorId.HasValue
            ? new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    utilizadorId.Value.ToString())
            }
            : Array.Empty<Claim>();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        claims,
                        utilizadorId.HasValue ? "Test" : null))
            }
        };

        return controller;
    }

    [Fact]
    public async Task SemParticipacoesAceites_CalculaRotaDireta()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        Assert.IsType<OkObjectResult>(resultado);

        // Sem pontos adicionais, apenas um segmento.
        Assert.Equal(1, handler.ChamadasRotas);

        // A viagem já tem as coordenadas guardadas.
        Assert.Equal(0, handler.ChamadasGeocodificacao);
    }

    [Fact]
    public async Task ParticipacaoPendente_NaoEntraNaRota()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = EstadoParticipacao.Pendente,
            Recolha = "Maia",
            Destino = "Trofa"
        });

        await f.Db.SaveChangesAsync();

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        Assert.IsType<OkObjectResult>(resultado);

        // A participação pendente não acrescenta paragens.
        Assert.Equal(1, handler.ChamadasRotas);
        Assert.Equal(0, handler.ChamadasGeocodificacao);
    }

    [Fact]
    public async Task ParticipacaoAceite_AdicionaPontosNaRota()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = EstadoParticipacao.Aceite,
            Recolha = "Maia",
            Destino = "Trofa"
        });

        await f.Db.SaveChangesAsync();

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        // Origem -> recolha -> destino do passageiro -> destino final.
        var dados = resposta.Value!;

        var pontos = Assert.IsAssignableFrom<
            IReadOnlyList<PontoRota>>(
            dados.GetType()
                .GetProperty("PontosOrdenados")!
                .GetValue(dados));

        Assert.Equal(4, pontos.Count);
        Assert.Equal("Origem", pontos[0].Tipo);
        Assert.Equal("Recolha", pontos[1].Tipo);
        Assert.Equal("DestinoPassageiro", pontos[2].Tipo);
        Assert.Equal("DestinoViagem", pontos[3].Tipo);

        Assert.Equal(2, handler.ChamadasGeocodificacao);
    }

    [Fact]
    public async Task UtilizadorSemPermissao_Devolve403()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var terceiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, terceiro.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        Assert.IsType<ForbidResult>(resultado);
        Assert.Equal(0, handler.ChamadasRotas);
    }


    [Fact]
    public async Task ParticipacaoAceiteSemPontos_UsaRotaDireta()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = EstadoParticipacao.Aceite,
            Recolha = null,
            Destino = null
        });

        await f.Db.SaveChangesAsync();

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, passageiro.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        var dados = resposta.Value!;

        var pontos = Assert.IsAssignableFrom<IReadOnlyList<PontoRota>>(
            dados.GetType()
                .GetProperty("PontosOrdenados")!
                .GetValue(dados));

        Assert.Equal(2, pontos.Count);
        Assert.Equal("Origem", pontos[0].Tipo);
        Assert.Equal("DestinoViagem", pontos[1].Tipo);

        Assert.Equal(1, handler.ChamadasRotas);
        Assert.Equal(0, handler.ChamadasGeocodificacao);
    }

    [Fact]
    public async Task RotaDireta_DevolveDistanciaEDuracaoCorretas()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        var resposta = Assert.IsType<OkObjectResult>(resultado);
        var dados = resposta.Value!;

        var distancia = (decimal)dados.GetType()
            .GetProperty("DistanciaTotalKm")!
            .GetValue(dados)!;

        var duracao = (int)dados.GetType()
            .GetProperty("DuracaoTotalMinutos")!
            .GetValue(dados)!;

        // O serviço simulado devolve 1000 metros e 120 segundos.
        Assert.Equal(1m, distancia);
        Assert.Equal(2, duracao);

        Assert.Equal(1, handler.ChamadasRotas);
    }
    [Fact]
    public async Task SemAutenticacao_Devolve401()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, null, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id, default);

        Assert.IsType<UnauthorizedResult>(resultado);
        Assert.Equal(0, handler.ChamadasRotas);
    }

    [Fact]
    public async Task ViagemInexistente_Devolve404()
    {
        using var f = new Fixture();

        var utilizador = await f.CriarUtilizador();
        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, utilizador.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            Guid.NewGuid(), default);

        Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.Equal(0, handler.ChamadasRotas);
    }
    [Fact]
    public async Task ServicoMapasIndisponivel_Devolve502()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var handler = new MapasSimulados
        {
            SimularFalha = true
        };

        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        var resposta = Assert.IsType<ObjectResult>(resultado);

        Assert.Equal(502, resposta.StatusCode);
    }

    [Fact]
    public async Task VariosPassageirosAceites_OrdenaPontosESomaSegmentos()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro1 = await f.CriarUtilizador();
        var passageiro2 = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        f.Db.Participacoes.AddRange(
            new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiro1.Id,
                Estado = EstadoParticipacao.Aceite,
                Recolha = "Maia",
                Destino = "Trofa"
            },
            new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiro2.Id,
                Estado = EstadoParticipacao.Aceite,
                Recolha = "Famalicão",
                Destino = "Vila Nova de Famalicão"
            });

        await f.Db.SaveChangesAsync();

        var handler = new MapasSimulados();
        var controller = CriarController(f.Db, condutor.Id, handler);

        var resultado = await controller.ConsultarRotaViagem(
            viagem.Id,
            default);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        var dados = resposta.Value!;

        var pontos = Assert.IsAssignableFrom<IReadOnlyList<PontoRota>>(
            dados.GetType()
                .GetProperty("PontosOrdenados")!
                .GetValue(dados));

        var segmentos = Assert.IsAssignableFrom<IReadOnlyList<SegmentoRota>>(
            dados.GetType()
                .GetProperty("Segmentos")!
                .GetValue(dados));

        Assert.Equal(6, pontos.Count);
        Assert.Equal(5, segmentos.Count);

        // Para cada passageiro, a recolha deve vir antes do destino.
        foreach (var passageiroId in new[]
        {
        f.Db.Participacoes.Local.First(p =>
            p.PassageiroId == passageiro1.Id).Id,

        f.Db.Participacoes.Local.First(p =>
            p.PassageiroId == passageiro2.Id).Id
    })
        {
            var indiceRecolha = pontos.ToList().FindIndex(
                p => p.ParticipacaoId == passageiroId &&
                     p.Tipo == "Recolha");

            var indiceDestino = pontos.ToList().FindIndex(
                p => p.ParticipacaoId == passageiroId &&
                     p.Tipo == "DestinoPassageiro");

            Assert.True(indiceRecolha >= 0);
            Assert.True(indiceDestino > indiceRecolha);
        }

        Assert.Equal(4, handler.ChamadasGeocodificacao);

        // O simulador devolve as mesmas coordenadas para
        // todos os pontos adicionais. Por isso, existem
        // segmentos com distância zero, sem chamadas HTTP.
        Assert.Equal(2, handler.ChamadasRotas);

        Assert.Equal(2m, (decimal)dados.GetType()
            .GetProperty("DistanciaTotalKm")!
            .GetValue(dados)!);

        Assert.Equal(4, (int)dados.GetType()
            .GetProperty("DuracaoTotalMinutos")!
            .GetValue(dados)!);
    }

}
