using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Validação real dos tokens, na API e no hub (TE-04, RNF-SEG-03, RNF-SEG-09, RF-F001-11).</summary>
/// <remarks>Sobre a <see cref="AplicacaoReal"/>: nenhum destes pedidos passa por um esquema de autenticação de teste.</remarks>
public sealed class AutenticacaoRealHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _utilizador;

    public AutenticacaoRealHttpTests() => _utilizador = _app.CriarUtilizador("Utilizador");

    public void Dispose() => _app.Dispose();

    private async Task<HttpStatusCode> ConsultarPerfilAsync(string token)
    {
        using var cliente = _app.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resposta = await cliente.GetAsync("/api/v1/autenticacao/me");
        return resposta.StatusCode;
    }

    private async Task<HttpStatusCode> NegociarComHubAsync(string? token)
    {
        using var cliente = _app.CreateClient();
        var query = token is null ? "" : $"&access_token={token}";
        using var resposta = await cliente.PostAsync($"/hubs/pedidos/negotiate?negotiateVersion=1{query}", null);
        return resposta.StatusCode;
    }

    private static string TokenSemAssinatura(Guid utilizadorId)
    {
        var expira = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var cabecalho = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var corpo = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            sub = utilizadorId,
            jti = Guid.NewGuid(),
            iss = AplicacaoReal.Emissor,
            aud = AplicacaoReal.Audiencia,
            exp = expira
        })));

        return $"{cabecalho}.{corpo}.";
    }

    private string TokenInvalido(string caso)
    {
        var validade = DateTime.UtcNow.AddHours(1);

        return caso switch
        {
            "assinatura" => AplicacaoReal.TokenManual(_utilizador.Id, validade, chave: new string('x', 48)),
            "emissor" => AplicacaoReal.TokenManual(_utilizador.Id, validade, emissor: "Outro"),
            "audiencia" => AplicacaoReal.TokenManual(_utilizador.Id, validade, audiencia: "Outra"),
            "sem-jti" => AplicacaoReal.TokenManual(_utilizador.Id, validade, comJti: false),
            "sem-assinatura" => TokenSemAssinatura(_utilizador.Id),
            "lixo" => "isto-nao-e-um-token",
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };
    }

    /// <summary>RNF-SEG-09: o token emitido pela própria API é aceite.</summary>
    [Fact]
    public async Task I110_74_TokenDaApi_EAceite()
    {
        Assert.Equal(HttpStatusCode.OK, await ConsultarPerfilAsync(_app.TokenDe(_utilizador)));
    }

    /// <summary>RNF-SEG-03: fora da validade somada à tolerância de 30 s de P-17, devolve 401.</summary>
    [Fact]
    public async Task I110_75_TokenExpirado_Devolve401()
    {
        var token = AplicacaoReal.TokenManual(_utilizador.Id, DateTime.UtcNow.AddSeconds(-40));

        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarPerfilAsync(token));
    }

    /// <summary>P-17: dentro da tolerância de relógio, o token ainda é aceite.</summary>
    [Fact]
    public async Task I110_76_TokenExpiradoDentroDaTolerancia_EAceite()
    {
        var token = AplicacaoReal.TokenManual(_utilizador.Id, DateTime.UtcNow.AddSeconds(-5));

        Assert.Equal(HttpStatusCode.OK, await ConsultarPerfilAsync(token));
    }

    /// <summary>RNF-SEG-09: qualquer token que a API não emitiu devolve 401, incluindo um sem <c>jti</c> (fim de sessão).</summary>
    [Theory]
    [InlineData("assinatura")]
    [InlineData("emissor")]
    [InlineData("audiencia")]
    [InlineData("sem-jti")]
    [InlineData("sem-assinatura")]
    [InlineData("lixo")]
    public async Task I110_77_TokenInvalido_Devolve401(string caso)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarPerfilAsync(TokenInvalido(caso)));
    }

    /// <summary>O hub aceita o token na query string, porque o WebSocket do browser não envia cabeçalhos.</summary>
    [Fact]
    public async Task I110_78_Hub_AceitaTokenNaQueryString()
    {
        Assert.Equal(HttpStatusCode.OK, await NegociarComHubAsync(_app.TokenDe(_utilizador)));
    }

    /// <summary>RNF-SEG-09: o hub recusa ligações sem token e com token inválido.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("assinatura")]
    [InlineData("sem-jti")]
    [InlineData("lixo")]
    public async Task I110_79_Hub_SemTokenValido_Devolve401(string? caso)
    {
        var token = caso is null ? null : TokenInvalido(caso);

        Assert.Equal(HttpStatusCode.Unauthorized, await NegociarComHubAsync(token));
    }

    /// <summary>DEF-BE-004: um token revogado também deixa de servir no hub.</summary>
    [Fact]
    public async Task I110_80_Hub_RecusaTokenRevogado()
    {
        var token = _app.TokenDe(_utilizador);
        using var cliente = _app.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var logout = await cliente.PostAsync("/api/v1/autenticacao/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await NegociarComHubAsync(token));
    }

    /// <summary>DC-46: o token na query string só vale no caminho do hub, porque fica nos logs de acesso.</summary>
    [Fact]
    public async Task I110_81_TokenNaQueryString_ForaDoHub_Devolve401()
    {
        using var cliente = _app.CreateClient();

        using var resposta = await cliente.GetAsync($"/api/v1/autenticacao/me?access_token={_app.TokenDe(_utilizador)}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    /// <summary>RF-F006-15, P-05: fluxo completo, do pedido do passageiro ao evento no hub do condutor, com token real.</summary>
    [Fact]
    public async Task I110_82_PedidoSubmetido_ChegaAoCondutorPeloHub()
    {
        var condutor = _app.CriarUtilizador("Condutor");
        var passageiro = _app.CriarUtilizador("Passageiro");
        using var clienteCondutor = _app.ClienteDe(condutor);
        using var clientePassageiro = _app.ClienteDe(passageiro);
        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);

        var recebido = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var servidor = _app.Server;
        await using var ligacao = new HubConnectionBuilder()
            .WithUrl(new Uri(servidor.BaseAddress, $"hubs/pedidos?access_token={_app.TokenDe(condutor)}"), opcoes =>
            {
                opcoes.HttpMessageHandlerFactory = _ => servidor.CreateHandler();
                opcoes.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        ligacao.On<JsonElement>("PedidoRecebido", evento => recebido.TrySetResult(evento.GetProperty("pedidoId").GetGuid()));
        await ligacao.StartAsync();

        var participacaoId = await AplicacaoReal.PedirParticipacaoAsync(clientePassageiro, viagemId);

        Assert.Equal(participacaoId, await recebido.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    /// <summary>RF-F001-11: email inexistente e palavra-passe errada devolvem exatamente a mesma resposta.</summary>
    [Fact]
    public async Task I110_83_Login_NaoRevelaQualDosCamposEstaErrado()
    {
        using var cliente = _app.CreateClient();

        using var emailInexistente = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = "ninguem@estg.ipp.pt", password = AplicacaoReal.PalavraPasse });
        using var palavraPasseErrada = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = _utilizador.Email, password = "Outra-palavra-passe-9" });

        Assert.Equal(HttpStatusCode.Unauthorized, emailInexistente.StatusCode);
        Assert.Equal(emailInexistente.StatusCode, palavraPasseErrada.StatusCode);
        Assert.Equal(
            await emailInexistente.Content.ReadAsStringAsync(),
            await palavraPasseErrada.Content.ReadAsStringAsync());
    }

    /// <summary>F001 de ponta a ponta: registo, login recusado por confirmar, confirmação, login e sessão válida.</summary>
    [Fact]
    public async Task I110_84_Registo_ConfirmacaoELogin_PelaApiReal()
    {
        using var cliente = _app.CreateClient();
        const string email = "novo.utilizador@estg.ipp.pt";
        var credenciais = new { email, password = AplicacaoReal.PalavraPasse };

        using var registo = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/registo",
            new { nome = "Novo", email, password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.Accepted, registo.StatusCode);

        using var porConfirmar = await cliente.PostAsJsonAsync("/api/v1/autenticacao/login", credenciais);
        Assert.Equal(HttpStatusCode.Forbidden, porConfirmar.StatusCode);

        using var confirmacao = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/confirmar",
            new { token = Assert.Single(_app.Email.Tokens) });
        Assert.Equal(HttpStatusCode.NoContent, confirmacao.StatusCode);

        using var login = await cliente.PostAsJsonAsync("/api/v1/autenticacao/login", credenciais);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, await ConsultarPerfilAsync(json.RootElement.GetProperty("accessToken").GetString()!));
    }
}
