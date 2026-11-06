using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Tests.Unit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>O que um browser precisa da API (DC-45, API-07, API-10, API-11, API-13, C1, DC-40).</summary>
/// <remarks>Sobre a <see cref="AplicacaoReal"/>, com a configuração do <c>appsettings.json</c>.</remarks>
public sealed class ProntoParaFrontendHttpTests
{
    private const string OrigemDoFrontend = "http://localhost:4200";

    /// <summary>Simula uma base de dados em baixo, recusando abrir ligações.</summary>
    private sealed class BaseDeDadosEmBaixo : DbConnectionInterceptor
    {
        public bool Ativo { get; set; }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            Ativo ? throw new InvalidOperationException("Base de dados em baixo.") : ValueTask.FromResult(result);
    }

    private static HttpRequestMessage Verificacao(string origem) => new(HttpMethod.Options, "/api/v1/pesquisa/viagens")
    {
        Headers =
        {
            { "Origin", origem },
            { "Access-Control-Request-Method", "GET" },
            { "Access-Control-Request-Headers", "authorization" }
        }
    };

    [Fact]
    public async Task I110_135_OrigemDoFrontend_PassaAVerificacaoDoBrowser()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();

        using var resposta = await cliente.SendAsync(Verificacao(OrigemDoFrontend));

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(OrigemDoFrontend, Assert.Single(resposta.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(resposta.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [Fact]
    public async Task I110_136_OutraOrigem_NaoEAutorizada()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();

        using var resposta = await cliente.SendAsync(Verificacao("https://outro-site.exemplo"));

        Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task I110_137_RespostaDeErro_TambemTrazOCabecalhoCors()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        cliente.DefaultRequestHeaders.Add("Origin", OrigemDoFrontend);

        using var resposta = await cliente.GetAsync("/api/v1/pesquisa/viagens");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.True(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task I110_138_ForaDeDesenvolvimento_RespostaHttpsTrazHsts()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://campuscarpool.exemplo")
        });

        using var resposta = await cliente.GetAsync("/health");

        Assert.True(resposta.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task I110_139_BaseDeDadosEmBaixo_HealthDevolve503()
    {
        var falha = new BaseDeDadosEmBaixo();
        using var app = new AplicacaoReal(services =>
            services.ConfigureDbContext<CampusCarpoolDbContext>(opcoes => opcoes.AddInterceptors(falha)));
        using var cliente = app.CreateClient();

        using (var normal = await cliente.GetAsync("/health"))
            Assert.Equal(HttpStatusCode.OK, normal.StatusCode);

        falha.Ativo = true;

        using var resposta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    [Fact]
    public async Task I110_140_DemasiadosPedidos_Devolve429ComMensagem()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();
        HttpResponseMessage? ultima = null;

        for (var i = 0; i < 11; i++)
        {
            ultima?.Dispose();
            ultima = await cliente.PostAsJsonAsync(
                "/api/v1/autenticacao/login", new { email = "alguem@estg.ipp.pt", password = "abcdefg1" });
        }

        using (ultima)
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);

            var corpo = await ultima.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Demasiados pedidos", corpo.GetProperty("erro").GetString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void I110_141_Hub_FechaQuandoOTokenExpira()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.CreateClient();

        var opcoes = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<HttpConnectionDispatcherOptions>())
            .First(metadados => metadados is not null);

        Assert.True(opcoes!.CloseOnAuthenticationExpiration);
    }

    [Fact]
    public async Task I110_142_Notificacao_TemTituloParaOUtilizador()
    {
        using var app = new AplicacaoReal();
        using var condutor = app.ClienteDe(app.CriarUtilizador("Condutor"));
        using var passageiro = app.ClienteDe(app.CriarUtilizador("Passageiro"));

        var viagem = await AplicacaoReal.CriarViagemAsync(condutor);
        await AplicacaoReal.PedirParticipacaoAsync(passageiro, viagem);

        var notificacao = app.NaBaseDeDados(db => db.Notificacoes.AsNoTracking().Single());

        Assert.Equal(TipoNotificacao.PedidoRecebido, notificacao.Tipo);
        Assert.Equal("Pedido recebido", notificacao.Titulo);
    }

    [Fact]
    public async Task I110_143_DuasNotificacoes_PedemUmSoTokenOAuth()
    {
        var (primeiro, segundo, pedidosOAuth) = await ServicoPushTests.EnviarDuasVezesAsync();

        Assert.Equal(1, pedidosOAuth);
        Assert.Equal(primeiro, segundo);
    }

    [Fact]
    public async Task I110_146_TokenFcmRecusado_PedeOutroENaoPerdeANotificacao()
    {
        var (pedidosOAuth, pedidosFcm) = await ServicoPushTests.EnviarComTokenRecusadoAsync();

        Assert.Equal(2, pedidosOAuth);
        Assert.Equal(2, pedidosFcm);
    }
}
