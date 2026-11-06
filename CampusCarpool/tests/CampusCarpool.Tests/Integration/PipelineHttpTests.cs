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

    /// <summary>Uma exceção inesperada dá 500 em ProblemDetails, sem a mensagem nem a stack trace.</summary>
    [Fact]
    public async Task I110_88_ExcecaoInesperada_Devolve500SemDetalhesInternos()
    {
        using var app = AplicacaoComFalha(new InvalidOperationException("segredo interno"));
        using var cliente = app.CreateClient();

        using var resposta = await IniciarSessaoAsync(app, cliente);

        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("segredo interno", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(HashQueFalha), corpo, StringComparison.Ordinal);
    }

    /// <summary>Um conflito de concorrência chega como <see cref="RegraDeNegocioException"/> e dá 409 com a mensagem.</summary>
    [Fact]
    public async Task I110_89_RegraDeNegocio_Devolve409ComAMensagem()
    {
        using var app = AplicacaoComFalha(new RegraDeNegocioException("Mensagem para o utilizador."));
        using var cliente = app.CreateClient();

        using var resposta = await IniciarSessaoAsync(app, cliente);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Mensagem para o utilizador.", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task I110_90_CorpoQueNaoEJson_Devolve400()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        using var conteudo = new StringContent("{ isto não é json", Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/v1/autenticacao/login", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>RNF-SEG-09: o estado do serviço responde sem token e não depende da base de dados.</summary>
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
    public async Task I110_92_RotaInexistente_Devolve404()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();

        using var resposta = await cliente.GetAsync("/api/v1/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
