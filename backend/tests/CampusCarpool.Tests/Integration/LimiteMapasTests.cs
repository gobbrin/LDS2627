using System.Diagnostics;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Comum;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Limite de 10 s de P-19 por operação (BE-11) e tamanho máximo dos textos enviados ao Serviço de Mapas (BE-30).
/// </summary>
/// <remarks>SQLite em memória com os serviços reais; OpenRouteService pelo <see cref="MapasFalsos"/>.</remarks>
public sealed class LimiteMapasTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly MapasFalsos mapas = new();
    private readonly CampusCarpoolDbContext db;

    public LimiteMapasTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao).Options);
        db.Database.CriarEsquemaDeTeste();
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    /// <summary>
    /// Criar uma viagem faz três pedidos aos mapas (duas geocodificações e uma rota). Com 4 s cada, a soma passaria os
    /// 10 s; a operação tem de terminar com 504 antes de 11 s.
    /// </summary>
    [Fact]
    public async Task I112_23_CriarViagemComMapasLentos_Devolve504AntesDe11Segundos()
    {
        mapas.Atraso = TimeSpan.FromSeconds(4);
        var servico = ServicosDeTeste.CriarViagens(db, mapas.CriarServico());
        var relogio = Stopwatch.StartNew();

        var resultado = await servico.CriarAsync(Guid.NewGuid(), "Porto", "Braga", DateTime.UtcNow.AddDays(1), 3, default);

        relogio.Stop();
        Assert.Equal(TipoErro.TempoEsgotado, resultado.Erro?.Tipo);
        Assert.Equal("Tempo de resposta do serviço de mapas excedido.", resultado.Erro?.Mensagem);
        Assert.True(relogio.Elapsed < TimeSpan.FromSeconds(11), $"Demorou {relogio.Elapsed.TotalSeconds:F1} s.");
        Assert.Empty(await db.Viagens.ToListAsync());
    }

    /// <summary>BE-30: um texto com mais de 200 caracteres é recusado sem chamar os mapas.</summary>
    [Fact]
    public async Task I112_24_GeocodificarTextoComMaisDe200Caracteres_Devolve400SemChamarOsMapas()
    {
        var controller = ServicosDeTeste.CriarRotasController(mapas.CriarServico(), ServicosDeTeste.CriarCustos());

        IActionResult resposta = await controller.Geocodificar(new string('a', 201), default);

        RespostasDeErro.AssertErro(resposta, 400, "O local tem no máximo 200 caracteres.");
        Assert.Equal(0, mapas.ChamadasGeocodificacao);
    }

    /// <summary>BE-30: o mesmo limite na origem e no destino da rota calculada.</summary>
    [Theory]
    [InlineData(201, 5, "A origem tem no máximo 200 caracteres.")]
    [InlineData(5, 201, "O destino tem no máximo 200 caracteres.")]
    public async Task I112_25_CalcularComTextoComMaisDe200Caracteres_Devolve400(int origem, int destino, string mensagem)
    {
        var controller = ServicosDeTeste.CriarRotasController(mapas.CriarServico(), ServicosDeTeste.CriarCustos());

        IActionResult resposta = await controller.Calcular(
            new RotasController.Pedido(new string('a', origem), new string('b', destino)),
            default);

        RespostasDeErro.AssertErro(resposta, 400, mensagem);
        Assert.Equal(0, mapas.ChamadasGeocodificacao);
    }
}
