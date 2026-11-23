using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Relógio parado que o teste avança à mão, para testar validades sem esperar.</summary>
public sealed class RelogioDeTeste : TimeProvider
{
    /// <summary>Momento atual do relógio.</summary>
    public DateTimeOffset Agora { get; set; } = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Agora;
}

/// <summary>Pedir bilhetes e ligar ao hub com eles, como os frontends vão fazer (DEF-BE-055).</summary>
public static class ClienteDoHub
{
    /// <summary>Pede um bilhete em <c>POST /autenticacao/bilhete-hub</c> com o token indicado e devolve-o.</summary>
    /// <param name="app">Aplicação de teste.</param>
    /// <param name="token">JWT da sessão.</param>
    public static async Task<string> PedirBilheteAsync(AplicacaoReal app, string token)
    {
        using HttpClient cliente = app.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage resposta = await cliente.PostAsync("/api/v1/autenticacao/bilhete-hub", null);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("bilhete").GetString()!;
    }

    /// <summary>Faz a negociação do SignalR com a query indicada (sem cabeçalho Authorization) e devolve a resposta.</summary>
    /// <param name="app">Aplicação de teste.</param>
    /// <param name="query">Parte da query a seguir a <c>negotiateVersion=1</c>, por exemplo <c>&amp;bilhete=…</c>.</param>
    public static async Task<HttpResponseMessage> NegociarAsync(AplicacaoReal app, string query)
    {
        using HttpClient cliente = app.CreateClient();
        return await cliente.PostAsync($"/hubs/pedidos/negotiate?negotiateVersion=1{query}", null);
    }

    /// <summary>
    /// Cria uma ligação ao hub, ainda fechada, com <c>?bilhete=</c> e só WebSocket, sem negociação: um único pedido
    /// HTTP, que gasta o bilhete. É o modo que os frontends usam (o WebSocket do browser não envia cabeçalhos).
    /// </summary>
    /// <param name="app">Aplicação de teste.</param>
    /// <param name="bilhete">Bilhete pedido com <see cref="PedirBilheteAsync"/>.</param>
    public static HubConnection CriarLigacao(AplicacaoReal app, string bilhete)
    {
        TestServer servidor = app.Server;

        HubConnection ligacao = new HubConnectionBuilder()
            .WithUrl(new Uri(servidor.BaseAddress, $"hubs/pedidos?bilhete={bilhete}"), opcoes =>
            {
                opcoes.SkipNegotiation = true;
                opcoes.Transports = HttpTransportType.WebSockets;
                // O servidor de testes não abre portas: o WebSocket passa pelo cliente dele, também sem cabeçalhos.
                opcoes.WebSocketFactory = async (contexto, ct) =>
                    await servidor.CreateWebSocketClient().ConnectAsync(contexto.Uri, ct);
            })
            .Build();

        // R5: os limites do próprio cliente passam a ser a espera máxima dos testes (máquina carregada).
        ligacao.HandshakeTimeout = AplicacaoReal.EsperaMaxima;
        ligacao.ServerTimeout = AplicacaoReal.EsperaMaxima;

        return ligacao;
    }
}

/// <summary>
/// DEF-BE-055: ligação ao hub com bilhete de uso único, em vez do JWT no endereço. Sobre a <see cref="AplicacaoReal"/>
/// (JwtBearer real).
/// </summary>
public sealed class BilheteHubHttpTests : IDisposable
{
    private readonly RelogioDeTeste _relogio = new();
    private readonly AplicacaoReal _app;
    private readonly Utilizador _utilizador;

    public BilheteHubHttpTests()
    {
        // O relógio dos bilhetes é o do teste; o resto da API usa o relógio real.
        _app = new AplicacaoReal(servicos =>
        {
            servicos.RemoveAll<BilhetesHub>();
            servicos.AddSingleton(new BilhetesHub(_relogio));
        });
        _utilizador = _app.CriarUtilizador("Utilizador");
    }

    public void Dispose() => _app.Dispose();

    /// <summary>O endpoint devolve só <c>{ bilhete, expiraEm }</c>, com a validade de 30 s em UTC.</summary>
    [Fact]
    public async Task I112_115_BilheteHub_DevolveBilheteEValidade()
    {
        using HttpClient cliente = _app.ClienteDe(_utilizador);

        using HttpResponseMessage resposta = await cliente.PostAsync("/api/v1/autenticacao/bilhete-hub", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(["bilhete", "expiraEm"], json.RootElement.EnumerateObject().Select(p => p.Name).Order());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("bilhete").GetString()));
        Assert.EndsWith("Z", json.RootElement.GetProperty("expiraEm").GetString());
        Assert.Equal(_relogio.Agora.AddSeconds(30).UtcDateTime, json.RootElement.GetProperty("expiraEm").GetDateTime().ToUniversalTime());
    }

    /// <summary>Sem sessão, o endpoint dá 401 sem corpo, como o resto da API.</summary>
    [Fact]
    public async Task I112_116_BilheteHub_SemSessao_401SemCorpo()
    {
        using HttpClient cliente = _app.CreateClient();

        using HttpResponseMessage resposta = await cliente.PostAsync("/api/v1/autenticacao/bilhete-hub", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("", await resposta.Content.ReadAsStringAsync());
    }

    /// <summary>O bilhete serve uma só vez: a segunda utilização dá 401 sem corpo.</summary>
    [Fact]
    public async Task I112_117_BilheteReutilizado_401()
    {
        string bilhete = await ClienteDoHub.PedirBilheteAsync(_app, _app.TokenDe(_utilizador));

        using HttpResponseMessage primeira = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");
        using HttpResponseMessage segunda = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, segunda.StatusCode);
        Assert.Equal("", await segunda.Content.ReadAsStringAsync());
    }

    /// <summary>O bilhete vale 30 s: a 29 s ainda liga, aos 30 s já dá 401.</summary>
    [Fact]
    public async Task I112_118_BilheteExpirado_401()
    {
        string token = _app.TokenDe(_utilizador);
        string dentroDoPrazo = await ClienteDoHub.PedirBilheteAsync(_app, token);
        string forDoPrazo = await ClienteDoHub.PedirBilheteAsync(_app, token);

        _relogio.Agora = _relogio.Agora.AddSeconds(29);
        using HttpResponseMessage aos29 = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={dentroDoPrazo}");

        _relogio.Agora = _relogio.Agora.AddSeconds(1);
        using HttpResponseMessage aos30 = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={forDoPrazo}");

        Assert.Equal(HttpStatusCode.OK, aos29.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, aos30.StatusCode);
    }

    /// <summary>
    /// O JWT em <c>?access_token=</c> deixou de ser aceite no hub, e um bilhete inventado também não serve: 401 sem
    /// corpo.
    /// </summary>
    [Theory]
    [InlineData("access_token")]
    [InlineData("bilhete")]
    public async Task I112_119_Hub_RecusaAccessTokenEBilheteInventado(string parametro)
    {
        string valor = parametro == "access_token" ? _app.TokenDe(_utilizador) : "bilhete-que-nunca-foi-emitido";

        using HttpResponseMessage resposta = await ClienteDoHub.NegociarAsync(_app, $"&{parametro}={valor}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("", await resposta.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// O bilhete só sai no corpo do endpoint que o emite: a negociação que o gasta não o devolve, e nem o bilhete nem
    /// o JWT ficam nos logs da aplicação.
    /// </summary>
    [Fact]
    public async Task I112_120_BilheteEJwt_NaoSaemNoutrasRespostasNemNosLogs()
    {
        string token = _app.TokenDe(_utilizador);
        string bilhete = await ClienteDoHub.PedirBilheteAsync(_app, token);

        using HttpResponseMessage negociacao = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");
        using HttpResponseMessage repetida = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");

        Assert.Equal(HttpStatusCode.OK, negociacao.StatusCode);
        Assert.DoesNotContain(bilhete, await negociacao.Content.ReadAsStringAsync());
        Assert.DoesNotContain(bilhete, await repetida.Content.ReadAsStringAsync());
        Assert.DoesNotContain(_app.Logs, linha => linha.Contains(bilhete) || linha.Contains(token));
    }
}
