using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Linha da rota para o mapa (RF-F008-07, DEF-BE-054, I112): pedida à HERE quando a rota é guardada, reutilizada
/// enquanto os pontos não mudam, com retas como reserva e escondida a quem não participa (P-13).
/// </summary>
/// <remarks>SQLite em memória com os serviços reais; OpenRouteService pelo <see cref="MapasFalsos"/> e HERE pela <see cref="HereFalsa"/>.</remarks>
public sealed class GeometriaRotaTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly MapasFalsos mapas = new();
    private readonly HereFalsa here = new();
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador condutor = new() { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador passageiro = new() { Nome = "Passageiro", Email = "passageiro@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador outro = new() { Nome = "Outro", Email = "outro@estg.ipp.pt", EmailConfirmado = true };

    public GeometriaRotaTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao).Options);
        db.Database.CriarEsquemaDeTeste();
        db.AddRange(condutor, passageiro, outro);
        db.SaveChanges();

        mapas.Locais["Porto"] = (41.15, -8.61);
        mapas.Locais["Braga"] = (41.55, -8.42);
        mapas.Locais["Maia"] = (41.23, -8.62);
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    private ViagemService Viagens(bool comHere = true) =>
        ServicosDeTeste.CriarViagens(db, mapas.CriarServico(), geometria: here.CriarServico(comHere));

    private RotasController Rotas(Guid utilizador)
    {
        var controller = ServicosDeTeste.CriarRotasController(
            mapas.CriarServico(), ServicosDeTeste.CriarCustos(), db, here.CriarServico());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, utilizador.ToString())], "Teste"))
            }
        };
        return controller;
    }

    private async Task<Guid> CriarViagemAsync(bool comHere = true)
    {
        var criada = await Viagens(comHere).CriarAsync(
            condutor.Id, "Porto", "Braga", DateTime.UtcNow.AddDays(2), 3, default);

        Assert.Null(criada.Erro);
        return criada.Valor.Id;
    }

    private async Task<ResultadoRotaComPontos> RotaGravadaAsync(Guid viagemId)
    {
        db.ChangeTracker.Clear();
        var viagem = await db.Viagens.AsNoTracking().SingleAsync(v => v.Id == viagemId);
        return RotaGuardada.Ler(viagem)!;
    }

    private async Task<RotaDaViagem> ConsultarAsync(Guid viagemId, Guid utilizador)
    {
        db.ChangeTracker.Clear();
        var resposta = Assert.IsType<OkObjectResult>(await Rotas(utilizador).ConsultarRotaViagem(viagemId, default));
        return Assert.IsType<RotaDaViagem>(resposta.Value);
    }

    // O passageiro pede recolha na Maia (paragem nova) e o condutor aceita.
    private async Task AceitarPassageiroNaMaiaAsync(Guid viagemId)
    {
        var participacoes = ServicosDeTeste.CriarParticipacoes(db, mapas.CriarServico(), geometria: here.CriarServico());
        var pedido = await participacoes.CriarPedidoAsync(viagemId, passageiro.Id, "Maia", null, default);
        Assert.Null(pedido.Erro);

        db.ChangeTracker.Clear();
        var aceite = await participacoes.AceitarAsync(pedido.Valor.Id, condutor.Id, default);
        Assert.Null(aceite.Erro);
    }

    [Fact]
    public async Task I112_02_CriarViagem_GuardaALinhaDaHere()
    {
        var viagemId = await CriarViagemAsync();

        var rota = await RotaGravadaAsync(viagemId);
        Assert.Equal(1, here.Chamadas);
        Assert.NotNull(rota.Geometria);
        Assert.Equal(4, rota.Geometria.Count);
        Assert.DoesNotContain("via=", here.UltimoPedido!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>Consultar a rota lê a linha guardada: não volta a chamar a HERE.</summary>
    public async Task I112_03_ConsultarRota_DevolveALinhaGuardadaSemChamarAHere()
    {
        var viagemId = await CriarViagemAsync();

        var rota = await ConsultarAsync(viagemId, condutor.Id);

        Assert.False(rota.GeometriaAproximada);
        Assert.Equal(4, rota.Geometria.Count);
        Assert.Equal([50.10228m, 8.69821m], rota.Geometria[0]);
        Assert.Equal(1, here.Chamadas);
    }

    [Fact]
    /// <summary>Sem chave da HERE (reserva): a viagem cria-se e o mapa recebe retas entre os pontos.</summary>
    public async Task I112_04_SemHere_AViagemCriaSeEARotaTemRetas()
    {
        var viagemId = await CriarViagemAsync(comHere: false);

        Assert.Null((await RotaGravadaAsync(viagemId)).Geometria);
        Assert.Equal(0, here.Chamadas);

        var rota = await ConsultarAsync(viagemId, condutor.Id);
        Assert.True(rota.GeometriaAproximada);
        Assert.Equal([[41.15m, -8.61m], [41.55m, -8.42m]], rota.Geometria);
    }

    [Fact]
    /// <summary>A HERE em erro nunca impede a operação: a viagem cria-se sem linha.</summary>
    public async Task I112_05_HereEmErro_AViagemCriaSeSemLinha()
    {
        here.Falha = System.Net.HttpStatusCode.InternalServerError;

        var viagemId = await CriarViagemAsync();

        Assert.Equal(1, here.Chamadas);
        Assert.Null((await RotaGravadaAsync(viagemId)).Geometria);
    }

    [Fact]
    /// <summary>Decisão de 16/11: alterar só a hora e os lugares não muda os pontos; a linha não se pede de novo.</summary>
    public async Task I112_06_AlterarSoAHora_ReutilizaALinha()
    {
        var viagemId = await CriarViagemAsync();
        db.ChangeTracker.Clear();

        var alterada = await Viagens().EditarAsync(
            viagemId, condutor.Id, DateTime.UtcNow.AddDays(3), 2, null, null, default);

        Assert.Null(alterada.Erro);
        Assert.Equal(1, here.Chamadas);
        Assert.Equal(4, (await RotaGravadaAsync(viagemId)).Geometria!.Count);
    }

    [Fact]
    /// <summary>Uma paragem nova muda os pontos: a linha pede-se de novo, com a paragem como <c>via</c>.</summary>
    public async Task I112_07_AceitarComParagemNova_PedeALinhaComVia()
    {
        var viagemId = await CriarViagemAsync();

        await AceitarPassageiroNaMaiaAsync(viagemId);

        Assert.Equal(2, here.Chamadas);
        Assert.Contains("via=41.23,-8.62", Uri.UnescapeDataString(here.UltimoPedido!.ToString()), StringComparison.Ordinal);

        var doCondutor = await ConsultarAsync(viagemId, condutor.Id);
        Assert.False(doCondutor.GeometriaAproximada);
        Assert.Equal(4, doCondutor.Geometria.Count);
    }

    [Fact]
    /// <summary>P-13: com paragens, quem não participa recebe só a reta da origem ao destino.</summary>
    public async Task I112_08_QuemNaoParticipa_NaoVeALinhaComParagens()
    {
        var viagemId = await CriarViagemAsync();
        Assert.False((await ConsultarAsync(viagemId, outro.Id)).GeometriaAproximada);

        await AceitarPassageiroNaMaiaAsync(viagemId);

        var rota = await ConsultarAsync(viagemId, outro.Id);
        Assert.True(rota.GeometriaAproximada);
        Assert.Equal([[41.15m, -8.61m], [41.55m, -8.42m]], rota.Geometria);
    }

    [Fact]
    /// <summary>A simulação não grava nada, por isso não pede a linha.</summary>
    public async Task I112_09_Simular_NaoChamaAHere()
    {
        var viagemId = await CriarViagemAsync();
        var participacoes = ServicosDeTeste.CriarParticipacoes(db, mapas.CriarServico(), geometria: here.CriarServico());
        var pedido = await participacoes.CriarPedidoAsync(viagemId, passageiro.Id, "Maia", null, default);

        var simulacao = await participacoes.SimularAsync(viagemId, pedido.Valor.Id, condutor.Id, default);

        Assert.Null(simulacao.Erro);
        Assert.Equal(1, here.Chamadas);
    }

    [Fact]
    /// <summary>Uma rota guardada antes da I112 (lista de pontos, sem linha) continua a ler-se.</summary>
    public void I112_10_RotaNoFormatoAntigo_LeSeSemLinha()
    {
        const string Antiga =
            "[{\"ordem\":0,\"tipo\":\"Origem\",\"participacaoId\":null,\"local\":\"Porto\",\"latitude\":41.15,\"longitude\":-8.61," +
            "\"adicional\":false,\"distanciaKm\":0,\"duracaoMinutos\":0}," +
            "{\"ordem\":1,\"tipo\":\"DestinoViagem\",\"participacaoId\":null,\"local\":\"Braga\",\"latitude\":41.55," +
            "\"longitude\":-8.42,\"adicional\":false,\"distanciaKm\":50,\"duracaoMinutos\":45}]";

        var rota = RotaGuardada.Ler(Antiga);

        Assert.Equal(2, rota.PontosOrdenados.Count);
        Assert.Equal(50m, rota.DistanciaTotalKm);
        Assert.Null(rota.Geometria);
        Assert.Equal(0, RotaGuardada.ContarAdicionais(Antiga));
    }

    [Fact]
    public async Task I112_11_PreVisualizacao_TrazALinhaDaHere()
    {
        var resposta = Assert.IsType<OkObjectResult>(
            await Rotas(condutor.Id).Calcular(new RotasController.Pedido("Porto", "Braga"), default));

        var rota = Assert.IsType<RotaCalculada>(resposta.Value);
        Assert.False(rota.GeometriaAproximada);
        Assert.Equal(4, rota.Geometria.Count);
        Assert.Contains("origin=41.15,-8.61", Uri.UnescapeDataString(here.UltimoPedido!.ToString()), StringComparison.Ordinal);
    }
}
