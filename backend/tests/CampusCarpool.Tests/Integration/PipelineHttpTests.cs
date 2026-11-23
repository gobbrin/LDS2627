using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>O pipeline do <c>Program.cs</c>: tratamento de erros registado, corpos inválidos e rotas sem sessão (TE-04).</summary>
public sealed class PipelineHttpTests
{
    private sealed class HashQueFalha(Exception erro) : IHashPalavraPasse
    {
        public string Gerar(Utilizador utilizador, string palavraPasse) => throw erro;

        public bool Verificar(Utilizador utilizador, string palavraPasse) => throw erro;
    }

    private static AplicacaoReal AplicacaoComFalha(Exception erro) => new(services =>
    {
        services.RemoveAll<IHashPalavraPasse>();
        services.AddSingleton<IHashPalavraPasse>(new HashQueFalha(erro));
    });

    private static Task<HttpResponseMessage> IniciarSessaoAsync(AplicacaoReal app, HttpClient cliente) =>
        cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = app.CriarUtilizador("Utilizador").Email, password = AplicacaoReal.PalavraPasse });

    /// <summary>
    /// Verifica que a resposta tem o código esperado e só o campo <c>erro</c>, com a mensagem esperada (BE-08).
    /// </summary>
    private static async Task AssertSoCampoErroAsync(HttpResponseMessage resposta, HttpStatusCode codigo, string mensagem)
    {
        string texto = await resposta.Content.ReadAsStringAsync();
        Assert.True(codigo == resposta.StatusCode, $"Esperado {(int)codigo}, recebido {(int)resposta.StatusCode}: {texto}");
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);

        using JsonDocument json = JsonDocument.Parse(texto);
        JsonProperty campo = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("erro", campo.Name);
        Assert.Equal(mensagem, campo.Value.GetString());
    }

    /// <summary>Uma exceção inesperada dá 500 com <c>{ erro }</c> genérico, sem a mensagem nem a stack trace (BE-08).</summary>
    [Fact]
    public async Task I110_88_ExcecaoInesperada_Devolve500SemDetalhesInternos()
    {
        using var app = AplicacaoComFalha(new InvalidOperationException("segredo interno"));
        using var cliente = app.CreateClient();

        using var resposta = await IniciarSessaoAsync(app, cliente);

        var corpo = await resposta.Content.ReadAsStringAsync();
        await AssertSoCampoErroAsync(resposta, HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado.");
        Assert.DoesNotContain("segredo interno", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(HashQueFalha), corpo, StringComparison.Ordinal);
    }

    /// <summary>
    /// Um conflito de concorrência chega como <see cref="RegraDeNegocioException"/> e dá 409 com a mensagem em
    /// <c>{ erro }</c> (BE-08).
    /// </summary>
    [Fact]
    public async Task I110_89_RegraDeNegocio_Devolve409ComAMensagem()
    {
        using var app = AplicacaoComFalha(new RegraDeNegocioException("Mensagem para o utilizador."));
        using var cliente = app.CreateClient();

        using var resposta = await IniciarSessaoAsync(app, cliente);

        await AssertSoCampoErroAsync(resposta, HttpStatusCode.Conflict, "Mensagem para o utilizador.");
    }

    /// <summary>BE-08: corpo que não é JSON dá 415 com <c>{ erro }</c> em português (DEF-BE-047).</summary>
    [Fact]
    public async Task I112_13_CorpoQueNaoEJson_Devolve415ComErro()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        using var conteudo = new StringContent("email=a", Encoding.UTF8, "text/plain");

        using var resposta = await cliente.PostAsync("/api/v1/autenticacao/login", conteudo);

        await AssertSoCampoErroAsync(
            resposta,
            HttpStatusCode.UnsupportedMediaType,
            "O corpo do pedido tem de ser JSON (Content-Type: application/json).");
    }

    /// <summary>BE-08: método errado num endereço que existe dá 405 com <c>{ erro }</c>.</summary>
    [Fact]
    public async Task I112_14_MetodoErrado_Devolve405ComErro()
    {
        using var app = new AplicacaoReal();
        using var comSessao = app.ClienteDe(app.CriarUtilizador("Utilizador"));

        using var resposta = await comSessao.DeleteAsync("/api/v1/autenticacao/login");

        await AssertSoCampoErroAsync(resposta, HttpStatusCode.MethodNotAllowed, "Método não permitido neste endereço.");
    }

    /// <summary>BE-25: um campo que o pedido não conhece, ou com o tipo errado, dá 400 em vez de ser ignorado.</summary>
    [Theory]
    [InlineData("{\"email\":\"a@ipca.pt\",\"password\":\"x\",\"admin\":true}")]
    [InlineData("{\"email\":123,\"password\":\"x\"}")]
    public async Task I112_15_CampoAMaisOuDeTipoErrado_Devolve400(string corpo)
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        using var conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/v1/autenticacao/login", conteudo);

        await AssertSoCampoErroAsync(
            resposta,
            HttpStatusCode.BadRequest,
            "Pedido inválido: campo em falta, a mais ou com tipo errado.");
    }

    [Fact]
    public async Task I110_90_CorpoQueNaoEJson_Devolve400()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        using var conteudo = new StringContent("{ isto não é json", Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/v1/autenticacao/login", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Pedido inválido: campo em falta, a mais ou com tipo errado.", json.RootElement.GetProperty("erro").GetString());
    }

    /// <summary>RNF-SEG-09: o estado do serviço responde sem token.</summary>
    [Theory]
    [InlineData("/health")]
    [InlineData("/")]
    public async Task I110_91_EstadoDoServico_RespondeSemToken(string caminho)
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();

        using var resposta = await cliente.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task I110_92_RotaInexistente_Devolve404ComSessaoE401Sem()
    {
        using var app = new AplicacaoReal();
        using var anonimo = app.CreateClient();
        using var comSessao = app.ClienteDe(app.CriarUtilizador("Utilizador"));

        using var semToken = await anonimo.GetAsync("/api/v1/nao-existe");
        using var comToken = await comSessao.GetAsync("/api/v1/nao-existe");

        // 401 sem corpo (exceção do contrato); 404 com { erro } (BE-08).
        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
        Assert.Equal(string.Empty, await semToken.Content.ReadAsStringAsync());
        await AssertSoCampoErroAsync(comToken, HttpStatusCode.NotFound, "Endereço não encontrado.");
    }
}
