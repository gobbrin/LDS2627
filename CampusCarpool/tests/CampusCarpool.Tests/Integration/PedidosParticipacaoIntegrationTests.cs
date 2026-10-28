using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CampusCarpool.Api.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;

namespace CampusCarpool.Tests.Integration;

public sealed class PedidosParticipacaoIntegrationTests
{
    private sealed class NotificadorPedidosTempoRealFalso : INotificadorPedidosTempoReal
    {
        public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;

        public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;

        public Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection =
            new("Data Source=:memory:");

        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            _connection.Open();

            var options =
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.EnsureCreated();
        }

        public async Task<Utilizador> CriarUtilizador(
            bool confirmado = true)
        {
            var utilizador = new Utilizador
            {
                Nome = "Utilizador de teste",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = confirmado
            };

            Db.Utilizadores.Add(utilizador);
            await Db.SaveChangesAsync();

            return utilizador;
        }

        public async Task<Viagem> CriarViagem(
            Guid condutorId,
            int lugares = 2,
            string estado = "Ativa",
            DateTime? partida = null)
        {
            var viagem = new Viagem
            {
                CondutorId = condutorId,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = partida ?? DateTime.UtcNow.AddDays(3),
                LugaresOferecidos = lugares,
                LugaresDisponiveis = lugares,
                Estado = estado
            };

            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();

            return viagem;
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }

    private static PedidosController CriarController(
        CampusCarpoolDbContext db,
        Guid? utilizadorId,
        HttpMessageHandler? handler = null)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = "chave-ficticia-apenas-para-testes"
            })
            .Build();

        var httpClient = handler is null
            ? new HttpClient()
            : new HttpClient(handler);

        httpClient.BaseAddress =
            new Uri("https://api.openrouteservice.org/");

        var mapas = new MapasService(httpClient, configuracao);

        var repositorioNotificacoes = new RepositorioNotificacoes(db);
        var notificacaoService = new NotificacaoService(repositorioNotificacoes);

        var controller = new PedidosController(db, mapas, notificacaoService, new NotificadorPedidosTempoRealFalso());

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
    private sealed class MapasSimulados : HttpMessageHandler
    {
        private readonly HttpStatusCode _codigo;
        private readonly bool _localEncontrado;

        public MapasSimulados(
            HttpStatusCode codigo = HttpStatusCode.OK,
            bool localEncontrado = true)
        {
            _codigo = codigo;
            _localEncontrado = localEncontrado;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var resposta = _localEncontrado
                ? """
              {
                "features": [
                  {
                    "geometry": {
                      "coordinates": [-8.6, 41.1]
                    }
                  }
                ]
              }
              """
                : """
              {
                "features": []
              }
              """;

            return Task.FromResult(new HttpResponseMessage(_codigo)
            {
                Content = new StringContent(
                    resposta,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
    [Fact]
    public async Task PedidoValido_CriaParticipacaoPendente()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var controller = CriarController(f.Db, passageiro.Id, new MapasSimulados());

        var resultado = await controller.Criar(
            new(viagem.Id, "Maia", "Trofa"),
            default);

        var resposta = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(201, resposta.StatusCode);

        var pedido = Assert.Single(await f.Db.Participacoes.ToListAsync());

        Assert.Equal(viagem.Id, pedido.ViagemId);
        Assert.Equal(passageiro.Id, pedido.PassageiroId);
        Assert.Equal(EstadoParticipacao.Pendente, pedido.Estado);
        Assert.Equal("Maia", pedido.Recolha);
        Assert.Equal("Trofa", pedido.Destino);

        // Criar um pedido pendente não ocupa lugares.
        Assert.Equal(2, viagem.LugaresDisponiveis);

        //Confirmar que foi criada uma notificação para o condutor.
        var notificacao = Assert.Single(await f.Db.Notificacoes.ToListAsync());

        Assert.Equal(condutor.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.PedidoRecebido, notificacao.Tipo);
    }

    [Fact]
    public async Task SemAutenticacao_Devolve401()
    {
        using var f = new Fixture();

        var controller = CriarController(f.Db, null);

        var resultado = await controller.Criar(
            new(Guid.NewGuid(), null, null),
            default);

        Assert.IsType<UnauthorizedResult>(resultado);
        Assert.Empty(f.Db.Participacoes);
    }

    [Fact]
    public async Task ContaNaoConfirmada_Devolve403()
    {
        using var f = new Fixture();

        var passageiro = await f.CriarUtilizador(false);
        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(Guid.NewGuid(), null, null),
            default);

        Assert.IsType<ForbidResult>(resultado);
    }

    [Fact]
    public async Task ViagemInexistente_Devolve404()
    {
        using var f = new Fixture();

        var passageiro = await f.CriarUtilizador();
        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(Guid.NewGuid(), null, null),
            default);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task ViagemIndisponivel_Devolve409()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();

        var viagem = await f.CriarViagem(
            condutor.Id,
            estado: "Indisponibilizada");

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task ViagemPassada_Devolve409()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();

        var viagem = await f.CriarViagem(
            condutor.Id,
            partida: DateTime.UtcNow.AddDays(-1));

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task CondutorNaoPodePedirNaPropriaViagem()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var controller = CriarController(f.Db, condutor.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task PedidoPendenteDuplicado_Devolve409()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = EstadoParticipacao.Pendente
        });

        await f.Db.SaveChangesAsync();

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<ConflictObjectResult>(resultado);
        Assert.Single(f.Db.Participacoes);
    }

    [Fact]
    public async Task SemLugaresDisponiveis_Devolve409()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var outroPassageiro = await f.CriarUtilizador();

        var viagem = await f.CriarViagem(
            condutor.Id,
            lugares: 1);

        f.Db.Participacoes.Add(new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = outroPassageiro.Id,
            Estado = EstadoParticipacao.Aceite
        });

        await f.Db.SaveChangesAsync();

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<ConflictObjectResult>(resultado);
        Assert.Single(f.Db.Participacoes);
    }

    [Fact]
    public async Task PontoRecolhaInvalido_Devolve400()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, "   ", "Braga"),
            default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(f.Db.Participacoes);
    }

    [Fact]
    public async Task PontosOpcionais_PermiteValoresNulos()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.Equal(
            201,
            Assert.IsType<ObjectResult>(resultado).StatusCode);

        var pedido = Assert.Single(f.Db.Participacoes);
        Assert.Null(pedido.Recolha);
        Assert.Null(pedido.Destino);
    }
    [Fact]
public async Task PontoInexistente_Devolve422()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);

    var controller = CriarController(
        f.Db,
        passageiro.Id,
        new MapasSimulados(localEncontrado: false));

    var resultado = await controller.Criar(
        new(viagem.Id, "Local inexistente", null),
        default);

    Assert.Equal(
        422,
        Assert.IsType<UnprocessableEntityObjectResult>(resultado)
            .StatusCode);

    Assert.Empty(f.Db.Participacoes);
}

[Fact]
public async Task ServicoMapasIndisponivel_Devolve502()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);

    var controller = CriarController(
        f.Db,
        passageiro.Id,
        new MapasSimulados(HttpStatusCode.ServiceUnavailable));

    var resultado = await controller.Criar(
        new(viagem.Id, "Maia", null),
        default);

    Assert.Equal(
        502,
        Assert.IsType<ObjectResult>(resultado).StatusCode);

    Assert.Empty(f.Db.Participacoes);
}

[Fact]
public async Task SemChaveMapas_ComPonto_Devolve503()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);

    var configuracao = new ConfigurationBuilder().Build();

    var mapas = new MapasService(
        new HttpClient(),
        configuracao);

    var repositorioNotificacoes = new RepositorioNotificacoes(f.Db);
    var notificacaoService = new NotificacaoService(repositorioNotificacoes);

    var controller = new PedidosController(f.Db, mapas, notificacaoService, new NotificadorPedidosTempoRealFalso());

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
                            passageiro.Id.ToString())
                    },
                    "Test"))
        }
    };

    var resultado = await controller.Criar(
        new(viagem.Id, "Maia", null),
        default);

    Assert.Equal(
        503,
        Assert.IsType<ObjectResult>(resultado).StatusCode);

    Assert.Empty(f.Db.Participacoes);
}
[Fact]
public async Task PedidosAtivosDuplicados_BaseDadosRejeitaSegundo()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);

    f.Db.Participacoes.Add(new Participacao
    {
        ViagemId = viagem.Id,
        PassageiroId = passageiro.Id,
        Estado = EstadoParticipacao.Pendente
    });

    await f.Db.SaveChangesAsync();

    f.Db.Participacoes.Add(new Participacao
    {
        ViagemId = viagem.Id,
        PassageiroId = passageiro.Id,
        Estado = EstadoParticipacao.Aceite
    });

    await Assert.ThrowsAsync<DbUpdateException>(
        () => f.Db.SaveChangesAsync());
}
}
