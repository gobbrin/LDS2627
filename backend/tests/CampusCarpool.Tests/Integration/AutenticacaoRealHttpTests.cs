using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
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

    // DEF-BE-055: o hub já não aceita o JWT no endereço; pede-se um bilhete com o token e negoceia-se com ele.
    private async Task<HttpStatusCode> NegociarComHubAsync(string token)
    {
        string bilhete = await ClienteDoHub.PedirBilheteAsync(_app, token);
        using var resposta = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");
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

    /// <summary>
    /// O hub aceita o bilhete pedido com o token na query string, porque o WebSocket do browser não envia cabeçalhos
    /// (DEF-BE-055: antes era o próprio token).
    /// </summary>
    [Fact]
    public async Task I110_78_Hub_AceitaTokenNaQueryString()
    {
        Assert.Equal(HttpStatusCode.OK, await NegociarComHubAsync(_app.TokenDe(_utilizador)));
    }

    /// <summary>
    /// RNF-SEG-09: o hub recusa ligações sem credencial, e um token inválido nem chega a obter bilhete (DEF-BE-055).
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("assinatura")]
    [InlineData("sem-jti")]
    [InlineData("lixo")]
    public async Task I110_79_Hub_SemTokenValido_Devolve401(string? caso)
    {
        if (caso is null)
        {
            using var semCredencial = await ClienteDoHub.NegociarAsync(_app, "");
            Assert.Equal(HttpStatusCode.Unauthorized, semCredencial.StatusCode);
            return;
        }

        using var cliente = _app.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenInvalido(caso));
        using var bilhete = await cliente.PostAsync("/api/v1/autenticacao/bilhete-hub", null);

        Assert.Equal(HttpStatusCode.Unauthorized, bilhete.StatusCode);
    }

    /// <summary>
    /// DEF-BE-004: um token revogado também deixa de servir no hub, mesmo com um bilhete pedido antes do fim de sessão
    /// (DEF-BE-055).
    /// </summary>
    [Fact]
    public async Task I110_80_Hub_RecusaTokenRevogado()
    {
        var token = _app.TokenDe(_utilizador);
        string bilhete = await ClienteDoHub.PedirBilheteAsync(_app, token);
        using var cliente = _app.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var logout = await cliente.PostAsync("/api/v1/autenticacao/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var resposta = await ClienteDoHub.NegociarAsync(_app, $"&bilhete={bilhete}");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
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
        await using HubConnection ligacao = await CriarLigacaoAoHubAsync(_app.TokenDe(condutor));
        ligacao.On<JsonElement>("PedidoRecebido", evento => recebido.TrySetResult(evento.GetProperty("pedidoId").GetGuid()));
        await LigarEEsperarPeloServidorAsync(ligacao);

        var participacaoId = await AplicacaoReal.PedirParticipacaoAsync(clientePassageiro, viagemId);

        Assert.Equal(participacaoId, await recebido.Task.WaitAsync(AplicacaoReal.EsperaMaxima));
    }

    /// <summary>
    /// Cria uma ligação ao hub, ainda fechada, como os frontends: pede um bilhete com o token e liga por WebSocket com
    /// <c>?bilhete=</c> (DEF-BE-055; antes era o token na query string, por long polling).
    /// </summary>
    /// <param name="token">Token com que a ligação entra.</param>
    private async Task<HubConnection> CriarLigacaoAoHubAsync(string token)
    {
        string bilhete = await ClienteDoHub.PedirBilheteAsync(_app, token);
        return ClienteDoHub.CriarLigacao(_app, bilhete);
    }

    /// <summary>
    /// Abre a ligação e só volta quando o servidor acabou de a registar (R1).
    /// </summary>
    /// <remarks>
    /// O <c>StartAsync</c> volta logo que o servidor responde ao handshake, mas o servidor só depois associa a ligação
    /// ao utilizador e corre o <c>OnConnectedAsync</c> do hub (que a guarda em <c>LigacoesPorSessao</c>). Com a máquina
    /// carregada, um evento ou um fim de sessão podia chegar antes disso e perder-se. O servidor só lê as mensagens do
    /// cliente depois do <c>OnConnectedAsync</c>; por isso a resposta a uma invocação, aqui de um método que o hub não
    /// tem, prova que o registo já está feito.
    /// </remarks>
    /// <param name="ligacao">Ligação criada por <see cref="CriarLigacaoAoHubAsync"/>.</param>
    private static async Task LigarEEsperarPeloServidorAsync(HubConnection ligacao)
    {
        await ligacao.StartAsync().WaitAsync(AplicacaoReal.EsperaMaxima);
        await Assert.ThrowsAsync<HubException>(() => ligacao.InvokeAsync("MetodoQueNaoExiste").WaitAsync(AplicacaoReal.EsperaMaxima));
    }

    /// <summary>
    /// BE-18, DEF-BE-056, RNF-SEG-03: o fim de sessão fecha logo a ligação ao hub aberta com o mesmo token, e um
    /// pedido seguinte já não chega a essa ligação.
    /// </summary>
    [Fact]
    public async Task I112_54_FimDeSessao_FechaLigacaoAoHub()
    {
        var condutor = _app.CriarUtilizador("Condutor");
        var passageiro = _app.CriarUtilizador("Passageiro");
        string tokenCondutor = _app.TokenDe(condutor);
        using var clienteCondutor = _app.CreateClient();
        clienteCondutor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCondutor);
        using var clientePassageiro = _app.ClienteDe(passageiro);
        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);

        var fechada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recebido = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using HubConnection ligacao = await CriarLigacaoAoHubAsync(tokenCondutor);
        ligacao.Closed += _ =>
        {
            fechada.TrySetResult();
            return Task.CompletedTask;
        };
        ligacao.On<JsonElement>("PedidoRecebido", evento => recebido.TrySetResult(evento.GetProperty("pedidoId").GetGuid()));
        await LigarEEsperarPeloServidorAsync(ligacao);

        using var logout = await clienteCondutor.PostAsync("/api/v1/autenticacao/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // A ligação fecha do lado do servidor, sem esperar pela expiração do token (60 min).
        await fechada.Task.WaitAsync(AplicacaoReal.EsperaMaxima);
        Assert.Equal(HubConnectionState.Disconnected, ligacao.State);

        // Um pedido feito depois já não chega a esta ligação.
        await AplicacaoReal.PedirParticipacaoAsync(clientePassageiro, viagemId);
        Assert.False(recebido.Task.IsCompleted);
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

        // O 401 do login traz { erro } (só o 401 do JwtBearer vem sem corpo) e é igual nos dois casos.
        await RespostasDeErro.AssertErroAsync(emailInexistente, HttpStatusCode.Unauthorized, "Credenciais inválidas.");
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
        await RespostasDeErro.AssertErroAsync(porConfirmar, HttpStatusCode.Forbidden, "Conta por confirmar.");

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
