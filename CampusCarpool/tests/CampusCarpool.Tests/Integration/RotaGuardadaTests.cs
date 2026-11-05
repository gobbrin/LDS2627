using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Rotas;
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
/// <remarks>SQLite em memória, com o <c>ParticipacaoService</c> e o <c>RotaService</c> reais; OpenRouteService simulado pelo <see cref="MapasFalsos"/>.</remarks>
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
        RotaGuardada.GuardarDireta(viagem, new ResultadoRota(
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
        ServicosDeTeste.CriarRotas(db, mapas.CriarServico()));

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
            ServicosDeTeste.CriarRotasController(mapas.CriarServico(), new EstimativaCustoService(configuracao), db),
            condutor.Id).ConsultarRotaViagem(viagem.Id, default);
    }

    private async Task<Viagem> ViagemGravada()
    {
        db.ChangeTracker.Clear();
        return await db.Viagens.AsNoTracking().SingleAsync();
    }

    private static List<string> Locais(Viagem v) =>
        RotaGuardada.Ler(v)!.PontosOrdenados.Select(p => p.Local).ToList();

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

        var controller = ComUtilizador(ServicosDeTeste.CriarSimulacoesController(db, mapas.CriarServico()), condutor.Id);
        var simulacao = Assert.IsType<SimulacoesController.RespostaSimulacao>(
            Assert.IsType<OkObjectResult>(await controller.Simular(
                viagem.Id, new SimulacoesController.SimularPedido(pedido.Id), default)).Value);

        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);

        Assert.Equal(simulacao.OrdemSimulada, Locais(await ViagemGravada()));
        Assert.Equal(3, simulacao.DuracaoSimuladaMinutos);
    }

    [Fact]
    /// <summary>RF-F006-02 / DEF-BE-019, P-23: quem cancela não é notificado; o condutor é.</summary>
    public async Task I110_42_CancelarParticipacaoAceite_NotificaOCondutor()
    {
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);
        db.Notificacoes.RemoveRange(db.Notificacoes);
        await db.SaveChangesAsync();

        await Servico().CancelarAsync(pedido.Id, passageiro.Id, default);

        var notificacao = Assert.Single(await db.Notificacoes.AsNoTracking().ToListAsync());
        Assert.Equal(condutor.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.AlteracaoViagem, notificacao.Tipo);
    }

    [Fact]
    public async Task I110_37_Indisponibilizar_RetiraOsPontosDaRota()
    {
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);
        db.ChangeTracker.Clear();

        var controller = ComUtilizador(ServicosDeTeste.CriarViagensController(db, mapas.CriarServico()), condutor.Id);
        Assert.IsType<NoContentResult>(await controller.Indisponibilizar(viagem.Id, default));

        Assert.Equal(["Porto", "Braga"], Locais(await ViagemGravada()));
    }

    private ViagensController ControladorViagens() => ComUtilizador(
        ServicosDeTeste.CriarViagensController(
            db,
            mapas.CriarServico(),
            notificacaoService: new NotificacaoService(new RepositorioNotificacoes(db), new FilaNotificacoesPushFalsa())),
        condutor.Id);

    private static ViagensController.EditarViagem Edicao(string? origem, string? destino) =>
        new(DateTime.UtcNow.AddDays(2), 3, origem, destino);

    [Fact]
    /// <summary>
    /// F3 / DEF-BE-007, RF-F002-07, P-20: o ponto omitido segue a nova origem, a rota é recalculada com o passageiro,
    /// a duração de P-04 muda e o passageiro é notificado.
    /// </summary>
    public async Task I110_38_AlterarOrigem_RecalculaARotaENotificaOPassageiro()
    {
        pedido.Recolha = null;
        pedido.RecolhaLatitude = null;
        pedido.RecolhaLongitude = null;
        await db.SaveChangesAsync();
        await Servico().AceitarAsync(pedido.Id, condutor.Id, default);
        mapas.Locais["Maia"] = (41.23, -8.62);
        db.ChangeTracker.Clear();

        var resultado = await ControladorViagens().Editar(viagem.Id, Edicao("Maia", null), default);

        Assert.IsType<NoContentResult>(resultado);
        var gravada = await ViagemGravada();
        Assert.Equal("Maia", gravada.Origem);
        Assert.Equal("Braga", gravada.Destino);
        Assert.Equal(41.23m, gravada.OrigemLatitude);
        Assert.Equal(["Maia", "Trofa", "Braga"], Locais(gravada));
        Assert.Equal(4, gravada.DuracaoMinutos);
        Assert.Equal(3, gravada.LugaresOferecidos);
        Assert.Equal(2, gravada.LugaresDisponiveis);
        Assert.Contains(await db.Notificacoes.ToListAsync(), n =>
            n.UtilizadorId == passageiro.Id && n.Tipo == TipoNotificacao.AlteracaoViagem);
    }

    [Fact]
    /// <summary>RF-F002-17: sem o Serviço de Mapas a alteração é recusada e a viagem fica como estava.</summary>
    public async Task I110_39_AlterarSemServicoDeMapas_ERecusadoENadaMuda()
    {
        mapas.SimularFalha = true;

        var resultado = await ControladorViagens().Editar(viagem.Id, Edicao(null, null), default);

        Assert.Equal(502, Assert.IsType<ObjectResult>(resultado).StatusCode);
        var gravada = await ViagemGravada();
        Assert.Equal(2, gravada.LugaresOferecidos);
        Assert.Equal("Porto", gravada.Origem);
    }

    [Theory]
    [InlineData(" braga ", null)]        // Mesmo texto que o destino atual.
    [InlineData("Lisboa", "LISBOA")]     // Mesmo texto entre si.
    [InlineData("Lisboa", "Capital")]    // Textos diferentes, coordenadas iguais.
    /// <summary>RF-F002-16: a alteração aplica a regra de «mesmo local» (D-008).</summary>
    public async Task I110_40_AlterarParaOMesmoLocal_Devolve400(string origem, string? destino)
    {
        mapas.Locais["Lisboa"] = (38.72, -9.14);
        mapas.Locais["Capital"] = (38.72, -9.14);

        var resultado = await ControladorViagens().Editar(viagem.Id, Edicao(origem, destino), default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Equal("Porto", (await ViagemGravada()).Origem);
    }
}
