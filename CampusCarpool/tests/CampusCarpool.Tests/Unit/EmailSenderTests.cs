using System.Net;
using System.Text;
using System.Text.Json;
using CampusCarpool.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class EmailSenderTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(responder(request));
    }

    private sealed class FakeFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static EmailSender Criar(string? key, string? from, string? url, Func<HttpRequestMessage, HttpResponseMessage> responder, string? name = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Brevo:ApiKey"] = key,
            ["Brevo:FromEmail"] = from,
            ["Brevo:FromName"] = name,
            ["Frontend:ConfirmUrl"] = url
        }).Build();
        return new EmailSender(config, new FakeFactory(new HttpClient(new FakeHandler(responder))));
    }

    private static bool ContemTexto(JsonElement elemento, string esperado) => elemento.ValueKind switch
    {
        JsonValueKind.String => elemento.GetString()?.Contains(esperado, StringComparison.Ordinal) == true,
        JsonValueKind.Object => elemento.EnumerateObject().Any(p => ContemTexto(p.Value, esperado)),
        JsonValueKind.Array => elemento.EnumerateArray().Any(v => ContemTexto(v, esperado)),
        _ => false
    };

    [Theory]
    [InlineData(null, "noreply@test.pt", "https://app.test/confirm")]
    [InlineData("key", null, "https://app.test/confirm")]
    [InlineData("key", "noreply@test.pt", null)]
    public async Task ConfiguracaoIncompletaLancaExcecao(string? key, string? from, string? url)
    {
        var service = Criar(key, from, url, _ => new HttpResponseMessage(HttpStatusCode.OK));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendConfirmationAsync("destino@test.pt", "token", CancellationToken.None));
    }

    [Theory]
    [InlineData("https://app.test/confirm", "?token=a%20b")]
    [InlineData("https://app.test/confirm?lang=pt", "&token=a%20b")]
    public async Task EnviaPedidoComLinkEscapado(string url, string sufixo)
    {
        string? payload = null;
        string? apiKey = null;
        Uri? uri = null;
        HttpMethod? method = null;
        var service = Criar("segredo", "noreply@test.pt", url, request =>
        {
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            apiKey = string.Join(",", request.Headers.GetValues("api-key"));
            uri = request.RequestUri;
            method = request.Method;
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        await service.SendConfirmationAsync("destino@test.pt", "a b", CancellationToken.None);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", uri?.ToString());
        Assert.Equal("segredo", apiKey);
        Assert.NotNull(payload);
        using var documento = JsonDocument.Parse(payload);
        Assert.True(ContemTexto(documento.RootElement, url + sufixo),
            "O link de confirmação não foi encontrado nos valores do JSON enviado.");
        Assert.Contains("destino@test.pt", payload);
        Assert.Contains("Campus Carpool", payload);
    }

    [Fact]
    public async Task NomeRemetentePersonalizadoEUsado()
    {
        string? payload = null;
        var service = Criar("key", "from@test.pt", "https://app.test/confirm", request =>
        {
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, "Equipa Campus");
        await service.SendConfirmationAsync("to@test.pt", "abc", CancellationToken.None);
        Assert.Contains("Equipa Campus", payload);
    }

    [Fact]
    public async Task RejeicaoDoFornecedorLancaHttpRequestException()
    {
        var service = Criar("key", "from@test.pt", "https://app.test/confirm", _ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.SendConfirmationAsync("to@test.pt", "abc", CancellationToken.None));
        Assert.Contains("400", ex.Message);
    }
}
