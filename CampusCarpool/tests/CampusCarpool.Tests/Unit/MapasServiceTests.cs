using System.Net;
using System.Text;
using CampusCarpool.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class MapasServiceTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(responder(request));
    }

    private static MapasService Criar(string? chave, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var values = new Dictionary<string, string?> { ["Maps:ApiKey"] = chave };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var http = new HttpClient(new FakeHandler(responder)) { BaseAddress = new Uri("https://example.test/") };
        return new MapasService(http, config);
    }

    private static HttpResponseMessage Json(string content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    [Fact]
    public void ConfiguradoRefletePresencaDaChave()
    {
        Assert.False(Criar(null, _ => Json("{}")).Configurado);
        Assert.False(Criar("  ", _ => Json("{}")).Configurado);
        Assert.True(Criar("teste", _ => Json("{}")).Configurado);
    }

    [Fact]
    public async Task GeocodificarSemChaveFalha()
    {
        var service = Criar(null, _ => Json("{}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GeocodificarAsync("Porto", CancellationToken.None));
    }

    [Fact]
    public async Task GeocodificarSemResultadosRetornaNull()
    {
        var service = Criar("chave", _ => Json("{\"features\":[]}"));
        Assert.Null(await service.GeocodificarAsync("Porto", CancellationToken.None));
    }

    [Fact]
    public async Task GeocodificarDevolveLatitudeLongitudeEEnviaPesquisaEscapada()
    {
        string? uri = null;
        var service = Criar("chave", request =>
        {
            uri = request.RequestUri?.ToString();
            return Json("{\"features\":[{\"geometry\":{\"coordinates\":[-8.61,41.15]}}]}");
        });
        var resultado = await service.GeocodificarAsync("Porto centro", CancellationToken.None);
        Assert.Equal(new Coordenadas(41.15m, -8.61m), resultado);
        Assert.NotNull(uri);
        Assert.Contains("Porto centro", Uri.UnescapeDataString(uri));
    }

    [Fact]
    public async Task GeocodificarErroHttpPropagaExcecao()
    {
        var service = Criar("chave", _ => Json("{}", HttpStatusCode.BadGateway));
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GeocodificarAsync("Porto", CancellationToken.None));
    }

    [Fact]
    public async Task CalcularSemChaveFalha()
    {
        var service = Criar(null, _ => Json("{}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalcularAsync(new(41m, -8m), new(42m, -9m), CancellationToken.None));
    }

    [Fact]
    public async Task CalcularSemRotasRetornaNull()
    {
        var service = Criar("chave", _ => Json("{\"features\":[]}"));
        Assert.Null(await service.CalcularAsync(new(41m, -8m), new(42m, -9m), CancellationToken.None));
    }

    [Fact]
    public async Task CalcularConverteMetrosEmQuilometrosEArredondaMinutos()
    {
        string? authorization = null;
        string? uri = null;
        var service = Criar(" chave ", request =>
        {
            authorization = string.Join(",", request.Headers.GetValues("Authorization"));
            uri = request.RequestUri?.ToString();
            return Json("{\"features\":[{\"properties\":{\"summary\":{\"distance\":12345,\"duration\":125}}}]}");
        });
        var origem = new Coordenadas(41m, -8m);
        var destino = new Coordenadas(42m, -9m);
        var resultado = await service.CalcularAsync(origem, destino, CancellationToken.None);
        Assert.NotNull(resultado);
        Assert.Equal(12.34m, resultado.DistanciaKm);
        Assert.Equal(3, resultado.DuracaoMinutos);
        Assert.Equal(origem, resultado.Origem);
        Assert.Equal(destino, resultado.Destino);
        Assert.Equal("chave", authorization);
        Assert.Contains("start=-8,41", uri);
        Assert.Contains("end=-9,42", uri);
    }

    [Fact]
    public async Task CalcularErroHttpPropagaExcecao()
    {
        var service = Criar("chave", _ => Json("{}", HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(() => service.CalcularAsync(new(41m, -8m), new(42m, -9m), CancellationToken.None));
    }
}
