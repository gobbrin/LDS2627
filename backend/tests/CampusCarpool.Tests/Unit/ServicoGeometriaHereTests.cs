using System.Net;
using System.Text;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Cliente do HERE Routing v8 com respostas HTTP simuladas (DEF-BE-054, I112).</summary>
/// <remarks>Qualquer falha da HERE devolve <c>null</c> (a rota fica com linhas retas) e nunca lança.</remarks>
public sealed class ServicoGeometriaHereTests
{
    private static readonly Coordenadas Porto = new(41.15m, -8.61m);
    private static readonly Coordenadas Maia = new(41.23m, -8.62m);
    private static readonly Coordenadas Braga = new(41.55m, -8.42m);

    private sealed class Respostas(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Uri> Pedidos { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Pedidos.Add(request.RequestUri!);
            return Task.FromResult(responder(request));
        }
    }

    private static ServicoGeometriaHere Criar(Respostas respostas, string? chave = "chave-de-teste") =>
        new(
            new HttpClient(respostas),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Here:ApiKey"] = chave }).Build(),
            NullLogger<ServicoGeometriaHere>.Instance);

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string UmaSeccao(string polilinha) =>
        "{\"routes\":[{\"sections\":[{\"polyline\":\"" + polilinha + "\"}]}]}";

    [Fact]
    public async Task SemChave_NaoFazPedidosEDevolveNull()
    {
        var respostas = new Respostas(_ => Json(UmaSeccao(HereFalsa.Polilinha)));
        var servico = Criar(respostas, chave: " ");

        Assert.False(servico.Configurado);
        Assert.Null(await servico.CalcularLinhaAsync([Porto, Braga], default));
        Assert.Empty(respostas.Pedidos);
    }

    [Fact]
    /// <summary>Pedido: origem, um <c>via</c> por paragem, destino, carro, linha e a chave; o ponto repetido não segue.</summary>
    public async Task Pedido_LevaOrigemParagensEDestino_SemPontosRepetidosSeguidos()
    {
        var respostas = new Respostas(_ => Json(UmaSeccao(HereFalsa.Polilinha)));

        await Criar(respostas).CalcularLinhaAsync([Porto, Porto, Maia, Braga], default);

        string pedido = Uri.UnescapeDataString(Assert.Single(respostas.Pedidos).ToString());
        Assert.StartsWith("https://router.hereapi.com/v8/routes?", pedido);
        Assert.Contains("transportMode=car", pedido);
        Assert.Contains("return=polyline", pedido);
        Assert.Contains("origin=41.15,-8.61", pedido);
        Assert.Contains("via=41.23,-8.62", pedido);
        Assert.Contains("destination=41.55,-8.42", pedido);
        Assert.Contains("apikey=chave-de-teste", pedido);
        Assert.Single(pedido.Split("via="), parte => parte.StartsWith("41.23", StringComparison.Ordinal));
        Assert.Equal(2, pedido.Split("via=").Length);
    }

    [Fact]
    /// <summary>
    /// O SQL Server devolve decimal(9,6) com zeros à direita (41.230000); o pedido à HERE leva
    /// o mesmo texto que em SQLite («41.23,-8.62»), sem esses zeros.
    /// </summary>
    public async Task Pedido_CoordenadasComZerosADireita_SaemSemOsZeros()
    {
        var respostas = new Respostas(_ => Json(UmaSeccao(HereFalsa.Polilinha)));
        // Os mesmos pontos de Porto, Maia e Braga, tal como o SQL Server os lê.
        var portoBd = new Coordenadas(41.150000m, -8.610000m);
        var maiaBd = new Coordenadas(41.230000m, -8.620000m);
        var bragaBd = new Coordenadas(41.550000m, -8.420000m);

        await Criar(respostas).CalcularLinhaAsync([portoBd, maiaBd, bragaBd], default);

        string pedido = Uri.UnescapeDataString(Assert.Single(respostas.Pedidos).ToString());
        Assert.Contains("origin=41.15,-8.61&", pedido);
        Assert.Contains("via=41.23,-8.62&", pedido);
        Assert.Contains("destination=41.55,-8.42&", pedido);
    }

    [Fact]
    /// <summary>Uma secção por troço: as linhas juntam-se sem repetir o ponto onde uma acaba e a seguinte começa.</summary>
    public async Task VariasSeccoes_JuntaAsLinhasSemRepetirOPontoDeJuncao()
    {
        // A segunda secção começa no último ponto da primeira (50.09878, 8.68752) e segue para (50.09878, 8.68852).
        string segunda = "BFs94xJg5g1BAoG";
        var respostas = new Respostas(_ => Json(
            "{\"routes\":[{\"sections\":[{\"polyline\":\"" + HereFalsa.Polilinha + "\"},{\"polyline\":\"" + segunda + "\"}]}]}"));

        var linha = await Criar(respostas).CalcularLinhaAsync([Porto, Maia, Braga], default);

        Assert.NotNull(linha);
        Assert.Equal(5, linha.Count);
        Assert.Equal(new Coordenadas(50.10228m, 8.69821m), linha[0]);
        Assert.Equal(new Coordenadas(50.09878m, 8.68852m), linha[^1]);
    }

    [Theory]
    [InlineData("{\"routes\":[]}")] // sem rota possível
    [InlineData("{\"notices\":[]}")] // sem «routes»
    [InlineData("{\"routes\":[{\"sections\":[{\"polyline\":\"!!\"}]}]}")] // linha inválida
    [InlineData("não é JSON")]
    public async Task RespostaSemLinha_DevolveNull(string json)
    {
        var respostas = new Respostas(_ => Json(json));

        Assert.Null(await Criar(respostas).CalcularLinhaAsync([Porto, Braga], default));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ErroHttp_DevolveNull(HttpStatusCode codigo)
    {
        var respostas = new Respostas(_ => new HttpResponseMessage(codigo));

        Assert.Null(await Criar(respostas).CalcularLinhaAsync([Porto, Braga], default));
    }

    [Fact]
    public async Task TempoEsgotadoOuSemRede_DevolveNull()
    {
        var tempo = new Respostas(_ => throw new TaskCanceledException("Tempo esgotado simulado."));
        var rede = new Respostas(_ => throw new HttpRequestException("Sem rede."));

        Assert.Null(await Criar(tempo).CalcularLinhaAsync([Porto, Braga], default));
        Assert.Null(await Criar(rede).CalcularLinhaAsync([Porto, Braga], default));
    }

    [Fact]
    public async Task UmSoPontoDistinto_NaoFazPedido()
    {
        var respostas = new Respostas(_ => Json(UmaSeccao(HereFalsa.Polilinha)));

        Assert.Null(await Criar(respostas).CalcularLinhaAsync([Porto, Porto], default));
        Assert.Empty(respostas.Pedidos);
    }
}
