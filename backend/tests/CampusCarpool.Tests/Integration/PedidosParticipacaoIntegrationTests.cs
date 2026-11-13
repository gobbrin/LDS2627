using System.Net;
using System.Security.Claims;
using System.Text;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Submissão de pedidos de participação (F004): pedido válido, autenticação, conta, estado da viagem, duplicados, lugares, pontos e notificação ao condutor.</summary>
/// <remarks>
/// SQLite em memória; <c>PedidosController</c> instanciado diretamente; mapas simulados por <c>HttpMessageHandler</c>; tempo real e push falsos.
/// </remarks>
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
                    .UseBaseDeTeste(_connection)
                    .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.CriarEsquemaDeTeste();
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
            EstadoViagem estado = EstadoViagem.Ativa,
            DateTime? partida = null)
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

        var mapas = new ServicoMapasRotas(httpClient, configuracao);

        var repositorioNotificacoes = new RepositorioNotificacoes(db);
        var filaPushFalsa = new FilaNotificacoesPushFalsa();

        var notificacaoService = new NotificacaoService(repositorioNotificacoes, filaPushFalsa, new UnidadeDeTrabalhoEfCore(db));

        var controller = new PedidosController(ServicosDeTeste.CriarParticipacoes(db, mapas, notificacaoService, new NotificadorPedidosTempoRealFalso()));

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
            // "Maia" e "Mesmo sítio da Maia" dão as mesmas coordenadas; os outros textos dão coordenadas próprias.
            var texto = Uri.UnescapeDataString(request.RequestUri!.Query);
            var coordenadas = texto.Contains("Maia", StringComparison.Ordinal)
                ? "[-8.6, 41.1]"
                : "[-8.7, 41.2]";

            var resposta = _localEncontrado
                ? $$"""
              {
                "features": [
                  {
                    "geometry": {
                      "coordinates": {{coordenadas}}
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
    /// <summary>A8 / DC-34: as coordenadas do ponto indicado ficam guardadas; o ponto omitido fica a null (P-20).</summary>
    public async Task I110_25_PedidoGuardaCoordenadasDoPontoIndicado()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarController(f.Db, passageiro.Id, new MapasSimulados())
            .Criar(new PedidosController.CriarPedido(viagem.Id, "Maia", null), default);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(resultado).StatusCode);
        var pedido = await f.Db.Participacoes.AsNoTracking().SingleAsync();
        Assert.Equal(41.1m, pedido.RecolhaLatitude);
        Assert.Equal(-8.6m, pedido.RecolhaLongitude);
        Assert.Null(pedido.DestinoLatitude);
        Assert.Null(pedido.DestinoLongitude);
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

        RespostaEsperada.Proibido(resultado);
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
            estado: EstadoViagem.Indisponibilizada);

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
    /// <summary>P-20 / DEF-BE-014: um pedido sem recolha nem destino é rejeitado (este teste fixava o contrário).</summary>
    public async Task I110_26_PedidoSemPontos_Devolve400()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var controller = CriarController(f.Db, passageiro.Id);

        var resultado = await controller.Criar(
            new(viagem.Id, null, null),
            default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(f.Db.Participacoes);
    }

    [Theory]
    [InlineData(" braga ", null)]            // RF-F004-18 e RF-F004-20: recolha no destino da viagem (texto).
    [InlineData(null, "PORTO")]              // RF-F004-18 e RF-F004-21: destino na origem da viagem (texto).
    [InlineData("Maia", "maia")]             // RF-F004-18: recolha e destino com o mesmo texto.
    [InlineData("Maia", "Mesmo sítio da Maia")] // RF-F004-18: textos diferentes, coordenadas iguais.
    /// <summary>DEF-BE-015: recusa pontos no mesmo local, pelo texto e pelas coordenadas (D-008).</summary>
    public async Task I110_27_PontosNoMesmoLocal_Devolve400(string? recolha, string? destino)
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarController(f.Db, passageiro.Id, new MapasSimulados())
            .Criar(new(viagem.Id, recolha, destino), default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(f.Db.Participacoes);
    }

    [Fact]
    public async Task PontosOpcionais_AceitaSoRecolhaOuSoDestino()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var outro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var soRecolha = await CriarController(f.Db, passageiro.Id, new MapasSimulados())
            .Criar(new(viagem.Id, "Maia", null), default);
        var soDestino = await CriarController(f.Db, outro.Id, new MapasSimulados())
            .Criar(new(viagem.Id, null, "Trofa"), default);

        Assert.Equal(201, Assert.IsType<ObjectResult>(soRecolha).StatusCode);
        Assert.Equal(201, Assert.IsType<ObjectResult>(soDestino).StatusCode);
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

        var mapas = new ServicoMapasRotas(
            new HttpClient(),
            configuracao);

        var repositorioNotificacoes = new RepositorioNotificacoes(f.Db);
        var filaPushFalsa = new FilaNotificacoesPushFalsa();

        var notificacaoService = new NotificacaoService(repositorioNotificacoes, filaPushFalsa, new UnidadeDeTrabalhoEfCore(f.Db));

        var controller = new PedidosController(ServicosDeTeste.CriarParticipacoes(f.Db, mapas, notificacaoService, new NotificadorPedidosTempoRealFalso()));

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
