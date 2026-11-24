using System.Net;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// HERE como Serviço de Mapas principal e OpenRouteService como reserva (D-5), e a memória das geocodificações
/// (D-3, BE-38 a), pelo <see cref="ServicoMapasComReserva"/> real.
/// </summary>
/// <remarks>
/// Os dois fornecedores são HttpMessageHandler falsos (<see cref="HereMapasFalsa"/> e <see cref="MapasFalsos"/>), que
/// contam os pedidos: cada teste prova quem foi chamado e quantas vezes, e lê o valor devolvido.
/// </remarks>
public sealed class MapasHereComReservaTests : IDisposable
{
    private readonly HereMapasFalsa here = new();
    private readonly MapasFalsos openRouteService = new();
    private readonly MemoryCache memoria = new(new MemoryCacheOptions { SizeLimit = 100 });
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador condutor = new() { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };

    public MapasHereComReservaTests()
    {
        here.Locais["Porto"] = (41.15, -8.61);
        here.Locais["Braga"] = (41.55, -8.42);
        openRouteService.Locais["Porto"] = (41.16, -8.62);
        openRouteService.Locais["Braga"] = (41.56, -8.43);

        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao).Options);
        db.Database.CriarEsquemaDeTeste();
        db.Add(condutor);
        db.SaveChanges();
    }

    public void Dispose()
    {
        memoria.Dispose();
        here.Dispose();
        openRouteService.Dispose();
        db.Dispose();
        ligacao.Dispose();
    }

    // O serviço que a aplicação regista: HERE, reserva e memória (a mesma em todo o teste).
    private ServicoMapasComReserva Mapas(bool hereConfigurada = true, bool reservaConfigurada = true) => new(
        here.CriarServico(hereConfigurada),
        openRouteService.CriarServico(reservaConfigurada),
        memoria,
        NullLogger<ServicoMapasComReserva>.Instance);

    [Fact]
    /// <summary>HERE a responder: geocodificação, rota e matriz vêm dela e o OpenRouteService nunca é chamado.</summary>
    public async Task I112_67_HereResponde_AReservaNaoEChamada()
    {
        ServicoMapasComReserva mapas = Mapas();

        Coordenadas? porto = await mapas.GeocodificarAsync("Porto", default);
        var rota = await mapas.CalcularAsync(new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m), default);
        var matriz = await mapas.CalcularMatrizAsync([new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m)], default);

        Assert.Equal(new Coordenadas(41.15m, -8.61m), porto);
        Assert.Equal(12.3m, rota!.DistanciaKm);
        Assert.Equal(120d, matriz!.DuracoesSegundos[0][1]);
        Assert.Equal(1, here.ChamadasGeocodificacao);
        Assert.Equal(1, here.ChamadasRota);
        Assert.Equal(1, here.ChamadasMatriz);
        Assert.Equal(0, openRouteService.ChamadasGeocodificacao);
        Assert.Equal(0, openRouteService.ChamadasRotas);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Unauthorized)]
    /// <summary>HERE em erro (429, 5xx ou chave recusada): a mesma operação faz-se no OpenRouteService e o valor é o dele.</summary>
    public async Task I112_68_HereEmErro_UsaAReserva(HttpStatusCode erro)
    {
        here.Falha = erro;
        ServicoMapasComReserva mapas = Mapas();

        Coordenadas? porto = await mapas.GeocodificarAsync("Porto", default);
        var matriz = await mapas.CalcularMatrizAsync([new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m)], default);

        Assert.Equal(new Coordenadas(41.16m, -8.62m), porto);
        Assert.Equal(openRouteService.SegundosPorSegmento, matriz!.DuracoesSegundos[0][1]);
        Assert.Equal(1, here.ChamadasGeocodificacao);
        Assert.Equal(1, here.ChamadasMatriz);
        Assert.Equal(1, openRouteService.ChamadasGeocodificacao);
        Assert.Equal(1, openRouteService.ChamadasRotas);
    }

    [Fact]
    /// <summary>A HERE esgota o seu tempo (5 s do HttpClient), mas o limite da operação não: usa-se a reserva.</summary>
    public async Task I112_69_HereSemResposta_UsaAReserva()
    {
        here.SimularTempoEsgotado = true;

        var rota = await Mapas().CalcularAsync(new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m), default);

        Assert.NotNull(rota);
        Assert.Equal(1m, rota.DistanciaKm);
        Assert.Null(rota.Geometria);
        Assert.Equal(1, here.ChamadasRota);
        Assert.Equal(1, openRouteService.ChamadasRotas);
    }

    [Fact]
    /// <summary>
    /// PM-1: a HERE recusa a chave (401) e não há reserva configurada: o erro segue (502), mas fica um aviso no log com o
    /// código HTTP e sem a chave.
    /// </summary>
    public async Task I112_125_HereRecusadaSemReserva_FicaAvisoNoLogSemAChave()
    {
        here.Falha = HttpStatusCode.Unauthorized;
        var logs = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var fabrica = LoggerFactory.Create(b => b.AddProvider(new RegistadorDeLogs(logs)));
        var mapas = new ServicoMapasComReserva(
            here.CriarServico(), openRouteService.CriarServico(false), memoria, fabrica.CreateLogger<ServicoMapasComReserva>());

        await Assert.ThrowsAsync<HttpRequestException>(() => mapas.GeocodificarAsync("Porto", default));

        string aviso = Assert.Single(logs, l => l.Contains("não há reserva configurada", StringComparison.Ordinal));
        Assert.Contains("Warning", aviso, StringComparison.Ordinal);
        Assert.Contains("Unauthorized", aviso, StringComparison.Ordinal);
        Assert.DoesNotContain("chave-here-ficticia", aviso, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>As duas falham: a API responde 502 com a mensagem dos mapas, como antes da D-5; houve um pedido a cada uma.</summary>
    public async Task I112_70_HereEReservaEmErro_Devolve502()
    {
        here.Falha = HttpStatusCode.ServiceUnavailable;
        openRouteService.SimularFalha = true;
        RotasController controller = ServicosDeTeste.CriarRotasController(Mapas(), ServicosDeTeste.CriarCustos());

        IActionResult resposta = await controller.Geocodificar("Porto", default);

        RespostasDeErro.AssertErro(resposta, 502, "Serviço de mapas indisponível.");
        Assert.Equal(1, here.ChamadasGeocodificacao);
        Assert.Equal(1, openRouteService.PedidosRecebidos);
    }

    [Fact]
    /// <summary>Sem <c>Here:ApiKey</c>, tudo vai ao OpenRouteService, como antes da I112; a HERE não recebe pedidos.</summary>
    public async Task I112_71_SemChaveDaHere_SoAReserva()
    {
        ServicoMapasComReserva mapas = Mapas(hereConfigurada: false);

        Coordenadas? porto = await mapas.GeocodificarAsync("Porto", default);

        Assert.True(mapas.Configurado);
        Assert.Equal(new Coordenadas(41.16m, -8.62m), porto);
        Assert.Equal(0, here.ChamadasGeocodificacao);
        Assert.Equal(1, openRouteService.ChamadasGeocodificacao);
    }

    [Fact]
    /// <summary>
    /// D-3: o mesmo texto (sem distinguir maiúsculas nem espaços nas pontas) geocodifica-se uma vez; um local não
    /// encontrado não fica guardado e volta a perguntar-se.
    /// </summary>
    public async Task I112_72_Geocodificacao_FicaEmMemoria()
    {
        ServicoMapasComReserva mapas = Mapas();

        Coordenadas? primeira = await mapas.GeocodificarAsync("Porto", default);
        Coordenadas? segunda = await mapas.GeocodificarAsync("  PORTO ", default);
        Assert.Null(await mapas.GeocodificarAsync("Sítio desconhecido", default));
        Assert.Null(await mapas.GeocodificarAsync("Sítio desconhecido", default));

        Assert.Equal(primeira, segunda);
        Assert.Equal(3, here.ChamadasGeocodificacao);
    }

    [Fact]
    /// <summary>
    /// BE-38 a: pré-visualizar (<c>POST /rotas/calcular</c>) e criar a viagem logo a seguir com os mesmos textos
    /// geocodifica uma só vez cada local. A rota direta da HERE traz a linha: não há pedido de linha à parte, e a
    /// viagem fica com a linha detalhada guardada.
    /// </summary>
    public async Task I112_73_PreVisualizarECriar_NaoRepeteAGeocodificacao()
    {
        ServicoMapasComReserva mapas = Mapas();
        var linhaAParte = new HereFalsa();
        RotasController rotas = ServicosDeTeste.CriarRotasController(mapas, ServicosDeTeste.CriarCustos(), db, linhaAParte.CriarServico());
        ViagemService viagens = ServicosDeTeste.CriarViagens(db, mapas, geometria: linhaAParte.CriarServico());

        var previa = Assert.IsType<RotaCalculada>(Assert.IsType<OkObjectResult>(
            await rotas.Calcular(new RotasController.Pedido("Porto", "Braga"), default)).Value);
        Resultado<ViagemCriada> criada = await viagens.CriarAsync(condutor.Id, "Porto", "Braga", DateTime.UtcNow.AddDays(2), 3, default);

        Assert.False(previa.GeometriaAproximada);
        Assert.Equal(4, previa.Geometria.Count);
        Assert.Null(criada.Erro);
        Assert.Equal(12.3m, criada.Valor.DistanciaKm);
        Assert.Equal(2, here.ChamadasGeocodificacao);
        Assert.Equal(2, here.ChamadasRota);
        Assert.Equal(0, linhaAParte.Chamadas);
        Assert.Equal(0, openRouteService.ChamadasGeocodificacao);

        db.ChangeTracker.Clear();
        Viagem gravada = await db.Viagens.AsNoTracking().SingleAsync();
        Assert.Equal(4, RotaGuardada.Ler(gravada)!.Geometria!.Count);
    }
}
