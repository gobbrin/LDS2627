using System.Net;
using System.Text;
using System.Text.Json;
using CampusCarpool.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Casos adicionais do <c>MapasService</c> (matriz) com respostas HTTP simuladas.</summary>
public sealed class MapasServiceCoberturaAdicionalTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(responder(request));
    }

    private static MapasService Criar(string? chave, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Maps:ApiKey"] = chave }).Build();
        return new MapasService(new HttpClient(new Handler(responder))
        {
            BaseAddress = new Uri("https://api.heigit.org/")
        }, config);
    }

    private static HttpResponseMessage Json(string conteudo, HttpStatusCode estado = HttpStatusCode.OK)
        => new(estado) { Content = new StringContent(conteudo, Encoding.UTF8, "application/json") };

    private static readonly Coordenadas[] Pontos = [new(41.15m, -8.61m), new(41.16m, -8.62m)];

    [Fact]
    public async Task MatrizSemChaveRejeitada()
    {
        var servico = Criar(null, _ => throw new HttpRequestException("Não deveria enviar HTTP"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servico.CalcularMatrizAsync(Pontos, CancellationToken.None));
    }

    [Fact]
    public async Task MatrizComUmPontoRejeitada()
    {
        var servico = Criar("chave", _ => throw new HttpRequestException("Não deveria enviar HTTP"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.CalcularMatrizAsync([Pontos[0]], CancellationToken.None));
    }

    [Fact]
    public async Task MatrizPreservaValoresNulosEEnviaMetricas()
    {
        string? pedido = null;
        var servico = Criar("chave", request =>
        {
            pedido = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json("{\"durations\":[[0,120],[null,0]],\"distances\":[[0,500],[null,0]]}");
        });
        var resultado = await servico.CalcularMatrizAsync(Pontos, CancellationToken.None);
        Assert.NotNull(resultado);
        Assert.Equal(120d, resultado.DuracoesSegundos[0][1]);
        Assert.Null(resultado.DuracoesSegundos[1][0]);
        Assert.Equal(500d, resultado.DistanciasMetros[0][1]);
        Assert.Null(resultado.DistanciasMetros[1][0]);
        Assert.NotNull(pedido);
        using var documento = JsonDocument.Parse(pedido);
        var raiz = documento.RootElement;
        Assert.Equal(2, raiz.GetProperty("locations").GetArrayLength());
        Assert.Contains(raiz.GetProperty("metrics").EnumerateArray(),
            item => item.GetString() == "duration");
        Assert.Contains(raiz.GetProperty("metrics").EnumerateArray(),
            item => item.GetString() == "distance");
    }

    [Fact]
    public async Task MatrizPropagaErroHttp()
    {
        var servico = Criar("chave", _ => Json("{}", HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            servico.CalcularMatrizAsync(Pontos, CancellationToken.None));
    }
}
