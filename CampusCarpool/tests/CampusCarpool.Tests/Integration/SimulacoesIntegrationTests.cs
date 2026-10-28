using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

/// <summary>
/// I040: integração entre controlador, EF Core (SQLite real em memória) e
/// serviço HTTP de mapas simulado. Não utiliza SQL Server nem a API externa.
/// Testes alinhados com o contrato corrigido: SimularPedido(Guid PedidoId).
/// </summary>
public sealed class SimulacoesIntegrationTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }
        public Utilizador Condutor { get; } = new() { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
        public Utilizador Passageiro { get; } = new() { Nome = "Passageiro", Email = "passageiro@estg.ipp.pt", EmailConfirmado = true };
        public Utilizador Terceiro { get; } = new() { Nome = "Terceiro", Email = "terceiro@estg.ipp.pt", EmailConfirmado = true };
        public Viagem Viagem { get; }
        public Participacao Pedido { get; }

        public Fixture()
        {
            _connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(_connection).Options);
            Db.Database.EnsureCreated();
            Db.Utilizadores.AddRange(Condutor, Passageiro, Terceiro);
            Viagem = new Viagem
            {
                CondutorId = Condutor.Id, Origem = "Porto", Destino = "Felgueiras",
                PartidaEm = DateTime.UtcNow.AddDays(3), LugaresOferecidos = 3,
                LugaresDisponiveis = 3, Estado = "Ativa"
            };
            Db.Viagens.Add(Viagem);
            Pedido = new Participacao
            {
                ViagemId = Viagem.Id, PassageiroId = Passageiro.Id,
                Estado = EstadoParticipacao.Pendente, Recolha = "Lousada", Destino = "Penafiel"
            };
            Db.Participacoes.Add(Pedido);
            Db.SaveChanges();
        }

        public SimulacoesController Controller(Guid? userId, bool mapasConfigurados = true, HttpMessageHandler? handler = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = mapasConfigurados ? "chave-falsa-so-testes" : ""
            }).Build();
            var http = new HttpClient(handler ?? new FakeMapasHandler())
            {
                BaseAddress = new Uri("https://api.heigit.org/openrouteservice/")
            };
            var controller = new SimulacoesController(Db, new MapasService(http, config));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = userId is Guid id
                        ? new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, id.ToString())
                        }, "Test"))
                        : new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
            return controller;
        }

        public void Dispose() { Db.Dispose(); _connection.Dispose(); }
    }

    /// <summary>Responde com coordenadas e matriz determinísticas, sem chamadas externas.</summary>
    private sealed class FakeMapasHandler : HttpMessageHandler
    {
        private readonly bool _falhar;
        public FakeMapasHandler(bool falhar = false) => _falhar = falhar;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_falhar) throw new HttpRequestException("Falha simulada dos mapas");
            if (request.RequestUri!.AbsolutePath.Contains("geocode/search"))
            {
                var query = Uri.UnescapeDataString(request.RequestUri.Query);
                var index = query.IndexOf("text=", StringComparison.Ordinal);
                var nome = index >= 0 ? query[(index + 5)..].Split('&')[0] : "";
                var pos = nome switch
                {
                    "Porto" => 1, "Felgueiras" => 2, "Lousada" => 3,
                    "Penafiel" => 4, "Amarante" => 5, "Trofa" => 6, _ => 7
                };
                return Json(new { features = new[] { new { geometry = new { coordinates = new[] { (double)pos, 41.0 } } } } });
            }
            if (request.RequestUri.AbsolutePath.Contains("matrix"))
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                var pontos = body.RootElement.GetProperty("locations").EnumerateArray()
                    .Select(x => x[0].GetDouble()).ToArray();
                // Matriz simétrica com custos positivos e diagonal zero.
                var duracoes = pontos.Select(a => pontos.Select(b => Math.Abs(a - b) * 60.0).ToArray()).ToArray();
                var distancias = pontos.Select(a => pontos.Select(b => Math.Abs(a - b) * 1000.0).ToArray()).ToArray();
                return Json(new { durations = duracoes, distances = distancias });
            }
            throw new InvalidOperationException("Chamada inesperada: " + request.RequestUri);
        }

        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task I040_01_SemIdentificacao_Devolve401()
    {
        using var f = new Fixture();
        Assert.IsType<UnauthorizedResult>(await f.Controller(null).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_02_ApenasCondutorPodeSimular()
    {
        using var f = new Fixture();
        Assert.IsType<ForbidResult>(await f.Controller(f.Passageiro.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        Assert.IsType<ForbidResult>(await f.Controller(f.Terceiro.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_03_PedidoInexistente_Devolve404()
    {
        using var f = new Fixture();
        Assert.IsType<NotFoundObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task I040_04_ViagemInexistente_Devolve404()
    {
        using var f = new Fixture();
        Assert.IsType<NotFoundObjectResult>(await f.Controller(f.Condutor.Id).Simular(Guid.NewGuid(),
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_05_PedidoDeOutraViagem_Devolve404()
    {
        using var f = new Fixture();
        var outra = new Viagem { CondutorId = f.Condutor.Id, Origem = "A", Destino = "B",
            PartidaEm = DateTime.UtcNow.AddDays(2), LugaresOferecidos = 2, LugaresDisponiveis = 2 };
        f.Db.Viagens.Add(outra);
        await f.Db.SaveChangesAsync();
        Assert.IsType<NotFoundObjectResult>(await f.Controller(f.Condutor.Id).Simular(outra.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Theory]
    [InlineData(EstadoParticipacao.Aceite)]
    [InlineData(EstadoParticipacao.Cancelada)]
    [InlineData(EstadoParticipacao.Rejeitada)]
    public async Task I040_06_PedidoNaoPendente_Devolve409(EstadoParticipacao estado)
    {
        using var f = new Fixture();
        f.Pedido.Estado = estado;
        await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_07_ViagemInativa_Devolve409()
    {
        using var f = new Fixture();
        f.Viagem.Estado = "Indisponibilizada";
        await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_08_SemPontos_Devolve400()
    {
        using var f = new Fixture();
        f.Pedido.Recolha = null;
        f.Pedido.Destino = null;
        await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
    }

    [Fact]
    public async Task I040_09_MapasNaoConfigurados_Devolve503()
    {
        using var f = new Fixture();
        var result = Assert.IsType<ObjectResult>(await f.Controller(f.Condutor.Id, false).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public async Task I040_10_FalhaHttpMapas_Devolve502()
    {
        using var f = new Fixture();
        var result = Assert.IsType<ObjectResult>(await f.Controller(f.Condutor.Id, true, new FakeMapasHandler(true))
            .Simular(f.Viagem.Id, new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public async Task I040_11_SimulacaoValida_NaoAlteraPersistencia()
    {
        using var f = new Fixture();
        var antesLugares = f.Viagem.LugaresDisponiveis;
        var antesEstado = f.Pedido.Estado;
        var result = Assert.IsType<OkObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        var resposta = Assert.IsType<SimulacoesController.RespostaSimulacao>(result.Value);
        Assert.Equal(f.Viagem.Id, resposta.ViagemId);
        Assert.Equal(f.Pedido.Id, resposta.PedidoId);
        Assert.Equal("Porto", resposta.OrdemSimulada[0]);
        Assert.Equal("Felgueiras", resposta.OrdemSimulada[^1]);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(antesLugares, (await f.Db.Viagens.SingleAsync()) .LugaresDisponiveis);
        Assert.Equal(antesEstado, (await f.Db.Participacoes.SingleAsync()).Estado);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task I040_12_P20_PontoOpcional(bool soRecolha)
    {
        using var f = new Fixture();
        if (soRecolha) f.Pedido.Destino = null;
        else f.Pedido.Recolha = null;
        await f.Db.SaveChangesAsync();
        var result = Assert.IsType<OkObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        var resposta = Assert.IsType<SimulacoesController.RespostaSimulacao>(result.Value);
        Assert.Equal(soRecolha ? "Lousada" : "Porto", resposta.RecolhaPassageiro);
        Assert.Equal(soRecolha ? "Felgueiras" : "Penafiel", resposta.DestinoPassageiro);
    }

    [Fact]
    public async Task I040_13_PassageirosAceitesEntramNaRotaOriginal()
    {
        using var f = new Fixture();
        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = f.Viagem.Id, PassageiroId = f.Terceiro.Id,
            Estado = EstadoParticipacao.Aceite, Recolha = "Amarante", Destino = "Trofa"
        });
        await f.Db.SaveChangesAsync();
        var result = Assert.IsType<OkObjectResult>(await f.Controller(f.Condutor.Id).Simular(f.Viagem.Id,
            new SimulacoesController.SimularPedido(f.Pedido.Id), default));
        var resposta = Assert.IsType<SimulacoesController.RespostaSimulacao>(result.Value);
        Assert.Contains("Amarante", resposta.OrdemOriginal);
        Assert.Contains("Trofa", resposta.OrdemOriginal);
        Assert.True(Array.IndexOf(resposta.OrdemOriginal.ToArray(), "Amarante") < Array.IndexOf(resposta.OrdemOriginal.ToArray(), "Trofa"));
    }
}
