using System.Net;
using System.Text.Json;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class ServicoEmailBrevoCoverageTests
{
    private sealed class RespostaFalsa(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private static ServicoEmail Criar(string? chave, string? remetente, string? url,
        Func<HttpRequestMessage, HttpResponseMessage> responder, string? nome = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Brevo:ApiKey"] = chave,
            ["Brevo:FromEmail"] = remetente,
            ["Brevo:FromName"] = nome,
            ["Frontend:ConfirmUrl"] = url
        }).Build();
        return new ServicoEmail(config, new HttpClient(new RespostaFalsa(responder)));
    }

    [Theory]
    [InlineData(null, "remetente@test.pt", "https://app.test/confirmar")]
    [InlineData("segredo", null, "https://app.test/confirmar")]
    [InlineData("segredo", "remetente@test.pt", null)]
    [InlineData(" ", "remetente@test.pt", "https://app.test/confirmar")]
    public async Task SemConfiguracaoObrigatoria_NaoEnvia(string? chave, string? remetente, string? url)
    {
        var enviado = false;
        var servico = Criar(chave, remetente, url, _ =>
        {
            enviado = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.EnviarConfirmacaoAsync("destino@test.pt", "token", CancellationToken.None));
        Assert.False(enviado);
    }

    [Theory]
    [InlineData("https://app.test/confirmar", "?token=a%20b")]
    [InlineData("https://app.test/confirmar?lang=pt", "&token=a%20b")]
    public async Task EnviaPedidoBrevoComTokenEscapado(string url, string sufixo)
    {
        string? corpo = null;
        var servico = Criar("segredo", "remetente@test.pt", url, pedido =>
        {
            Assert.Equal(HttpMethod.Post, pedido.Method);
            Assert.Equal("https://api.brevo.com/v3/smtp/email", pedido.RequestUri?.ToString());
            Assert.Equal("segredo", Assert.Single(pedido.Headers.GetValues("api-key")));
            corpo = pedido.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        await servico.EnviarConfirmacaoAsync("destino@test.pt", "a b", CancellationToken.None);
        using var json = JsonDocument.Parse(Assert.IsType<string>(corpo));
        Assert.Equal("Campus Carpool", json.RootElement.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("remetente@test.pt", json.RootElement.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("destino@test.pt", json.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains(url + sufixo, json.RootElement.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task UsaNomeDeRemetenteConfigurado()
    {
        string? corpo = null;
        var servico = Criar("segredo", "remetente@test.pt", "https://app.test/confirmar", pedido =>
        {
            corpo = pedido.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, "Equipa Campus");
        await servico.EnviarConfirmacaoAsync("destino@test.pt", "token", CancellationToken.None);
        using var json = JsonDocument.Parse(Assert.IsType<string>(corpo));
        Assert.Equal("Equipa Campus", json.RootElement.GetProperty("sender").GetProperty("name").GetString());
    }

    [Fact]
    public async Task RespostaNaoBemSucedida_PropagaErroHttp()
    {
        var servico = Criar("segredo", "remetente@test.pt", "https://app.test/confirmar",
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var excecao = await Assert.ThrowsAsync<HttpRequestException>(() =>
            servico.EnviarConfirmacaoAsync("destino@test.pt", "token", CancellationToken.None));
        Assert.Contains("503", excecao.Message);
    }
}
