using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
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
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Rota guardada na viagem (F2, RF-F008-05, P-19, D-025): quando é recalculada e o que acontece sem o Serviço de Mapas.</summary>
/// <remarks>SQLite em memória, com o <c>ParticipacaoService</c> e o <c>RotaViagemService</c> reais; OpenRouteService simulado pelo <see cref="MapasFalsos"/>.</remarks>
public sealed class RotaGuardadaTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly MapasFalsos mapas = new();
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador condutor = new() { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador passageiro = new() { Nome = "Passageiro", Email = "passageiro@estg.ipp.pt", EmailConfirmado = true };
    private readonly Viagem viagem;
    private readonly Participacao pedido;

    public RotaGuardadaTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseSqlite(ligacao).Options);
        db.Database.EnsureCreated();

        viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            OrigemLatitude = 41.15m,
            OrigemLongitude = -8.61m,
            DestinoLatitude = 41.55m,
            DestinoLongitude = -8.42m,
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 2,
            LugaresDisponiveis = 2,
            DuracaoMinutos = 60
        };
        RotaViagemService.GuardarDireta(viagem, new ResultadoRota(
            50m,
            60,
            new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude),
            new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude)));

        pedido = new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Recolha = "Maia",
            RecolhaLatitude = 41.23m,
            RecolhaLongitude = -8.62m,
            Destino = "Trofa",
            DestinoLatitude = 41.33m,
            DestinoLongitude = -8.56m
        };

        db.AddRange(condutor, passageiro, viagem, pedido);
        db.SaveChanges();
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    private ParticipacaoService Servico() => new(
        new RepositorioParticipacoes(db),
        new RepositorioViagens(db),
        new UnidadeDeTrabalhoEfCore(db),
        new NotificacaoService(new RepositorioNotificacoes(db), new FilaNotificacoesPushFalsa()),
        new NotificadorPedidosFalso(),
        new RotaViagemService(mapas.CriarServico()));

    private static T ComUtilizador<T>(T controller, Guid id) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "Teste"))
            }
        };
        return controller;
    }

    private Task<IActionResult> ConsultarRota()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Custos:PrecoPorKm"] = "0.10" })
            .Build();

        return ComUtilizador(
            new RotasController(mapas.CriarServico(), new EstimativaCustoService(configuracao), db),
            condutor.Id).ConsultarRotaViagem(viagem.Id, default);
    }

    private async Task<Viagem> ViagemGravada()
    {
        db.ChangeTracker.Clear();
        return await db.Viagens.AsNoTracking().SingleAsync();
    }

    private static List<string> Locais(Viagem v) =>
        RotaViagemService.Ler(v)!.PontosOrdenados.Select(p => p.Local).ToList();

    [Fact]
    public async Task I110_31_ConsultaLeARotaGuardada_SemChamarOServicoDeMapas()
    {
        mapas.SimularFalha = true;

        Assert.IsType<OkObjectResult>(await ConsultarRota());

        Assert.Equal(0, mapas.ChamadasRotas);
        Assert.Equal(0, mapas.ChamadasGeocodificacao);
    }

    [Fact]
    public async Task I110_32_Aceitar_RecalculaARotaEMantemADuracaoDeP04()
    {
        var resultado = await Servico().AceitarAsync(pedido.Id, condutor.Id, default);

        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, resultado);
        var gravada = await ViagemGravada();
        Assert.Equal(["Porto", "Maia", "Trofa", "Braga"], Locais(gravada));
        Assert.Equal(3m, gravada.DistanciaKm);
        Assert.Equal(60, gravada.DuracaoMinutos);
        Assert.Equal(1, mapas.ChamadasRotas);
    }

    [Fact]
    public async Task I110_33_AceitarSemServicoDeMapas_ERecusadoENadaMuda()
    {
        mapas.SimularFalha = true;

        var resultado = await Servico().AceitarAsync(pedido.Id, condutor.Id, default);

        Assert.Equal(ResultadoGestaoParticipacao.RotaIndisponivel, resultado);
        var gravada = await ViagemGravada();
        Assert.Equal(2, gravada.LugaresDisponiveis);
        Assert.Equal(["Porto", "Braga"], Locais(gravada));
        Assert.Equal(EstadoParticipacao.Pendente, (await db.Participacoes.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task I110_34_Cancelar_RecalculaARotaSemOsPontosDoPassageiro()
    {
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);

        var resultado = await Servico().CancelarAsync(pedido.Id, passageiro.Id, default);

        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, resultado);
        Assert.Equal(["Porto", "Braga"], Locais(await ViagemGravada()));
    }

    [Fact]
    public async Task I110_35_CancelarSemServicoDeMapas_NuncaERecusadoEARotaFicaPorRecalcular()
    {
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);
        mapas.SimularFalha = true;

        var resultado = await Servico().CancelarAsync(pedido.Id, passageiro.Id, default);

        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, resultado);
        var gravada = await ViagemGravada();
        Assert.Null(gravada.RotaPontos);
        Assert.Equal(2, gravada.LugaresDisponiveis);

        // Enquanto o serviço falhar, a rota antiga (com o passageiro que saiu) não é mostrada.
        Assert.Equal(502, Assert.IsType<ObjectResult>(await ConsultarRota()).StatusCode);

        // A consulta seguinte em que o serviço responde recalcula e grava.
        mapas.SimularFalha = false;
        Assert.IsType<OkObjectResult>(await ConsultarRota());
        Assert.Equal(["Porto", "Braga"], Locais(await ViagemGravada()));
    }

    [Fact]
    public async Task I110_36_ARotaDepoisDeAceitarCoincideComASimulacao()
    {
        // Índices com um passageiro: 0 origem, 1 recolha, 2 destino, 3 destino da viagem.
        mapas.Duracao = (i, j) => (i, j) is (0, 1) or (1, 2) or (2, 3) ? 60 : 3000;

        var controller = ComUtilizador(new SimulacoesController(db, mapas.CriarServico()), condutor.Id);
        var simulacao = Assert.IsType<SimulacoesController.RespostaSimulacao>(
            Assert.IsType<OkObjectResult>(await controller.Simular(
                viagem.Id, new SimulacoesController.SimularPedido(pedido.Id), default)).Value);

        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);

        Assert.Equal(simulacao.OrdemSimulada, Locais(await ViagemGravada()));
        Assert.Equal(3, simulacao.DuracaoSimuladaMinutos);
    }

    [Fact]
    public async Task I110_37_Indisponibilizar_RetiraOsPontosDaRota()
    {
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);
        db.ChangeTracker.Clear();

        var controller = ComUtilizador(new ViagensController(db, mapas.CriarServico()), condutor.Id);
        Assert.IsType<NoContentResult>(await controller.Indisponibilizar(viagem.Id, default));

        Assert.Equal(["Porto", "Braga"], Locais(await ViagemGravada()));
    }
}
