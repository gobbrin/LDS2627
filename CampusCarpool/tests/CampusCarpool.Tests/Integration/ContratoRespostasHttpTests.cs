using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Nomes dos campos das respostas, tal como saem do pipeline HTTP com o serializador real da API.
/// </summary>
/// <remarks>
/// Os outros testes leem o objeto devolvido pelo controller, por isso não veem o JSON. Estes prendem o contrato: trocar
/// um tipo anónimo ou uma entidade por um DTO não pode mudar nada aqui. A autenticação é a de teste (cabeçalho
/// <c>X-Test-User-Id</c>), a BD é SQLite e o Serviço de Mapas é o <see cref="MapasFalsos"/>.
/// </remarks>
public sealed class ContratoRespostasHttpTests : IDisposable
{
    private const string PalavraPasse = "PalavraPasse123";

    private sealed class AutenticacaoDeTeste(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-User-Id"].ToString(), out var id))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identidade = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name)));
        }
    }

    private sealed class ApiFactory(SqliteConnection ligacao) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(local);Database=CampusCarpool_Tests;");
            builder.UseSetting("Jwt:Key", "CHAVE_DE_TESTES_I110_COM_MAIS_DE_32_BYTES_12345");
            builder.UseSetting("Custos:PrecoPorKm", "0.25");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<DbContextOptions<CampusCarpoolDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CampusCarpoolDbContext>>();
                services.AddDbContext<CampusCarpoolDbContext>(options => options.UseSqlite(ligacao));

                services.RemoveAll<IServicoMapasRotas>();
                services.AddSingleton<IServicoMapasRotas>(new MapasFalsos().CriarServico());

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Teste";
                    options.DefaultChallengeScheme = "Teste";
                })
                .AddScheme<AuthenticationSchemeOptions, AutenticacaoDeTeste>("Teste", _ => { });
            });
        }
    }

    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly ApiFactory factory;
    private readonly HttpClient cliente;
    private readonly Utilizador condutor = NovoUtilizador("Condutor");
    private readonly Utilizador passageiro = NovoUtilizador("Passageiro");

    public ContratoRespostasHttpTests()
    {
        ligacao.Open();
        factory = new ApiFactory(ligacao);
        cliente = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<IHashPalavraPasse>();

        db.Database.EnsureCreated();
        condutor.PasswordHash = hash.Gerar(condutor, PalavraPasse);
        passageiro.PasswordHash = hash.Gerar(passageiro, PalavraPasse);
        db.Utilizadores.AddRange(condutor, passageiro);
        db.SaveChanges();
    }

    public void Dispose()
    {
        cliente.Dispose();
        factory.Dispose();
        ligacao.Dispose();
    }

    private static Utilizador NovoUtilizador(string nome) => new()
    {
        Nome = nome,
        Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
        EmailConfirmado = true
    };

    private static void AssertCampos(JsonElement objeto, params string[] esperados) =>
        Assert.Equal(
            esperados.Order(StringComparer.Ordinal),
            objeto.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string caminho, Guid? utilizador, object? corpo = null)
    {
        using var pedido = new HttpRequestMessage(metodo, new Uri(caminho, UriKind.Relative));

        if (utilizador is Guid id) pedido.Headers.Add("X-Test-User-Id", id.ToString());
        if (corpo is not null) pedido.Content = JsonContent.Create(corpo);

        return await cliente.SendAsync(pedido);
    }

    private async Task<JsonElement> JsonAsync(HttpStatusCode esperado, HttpMethod metodo, string caminho, Guid? utilizador, object? corpo = null)
    {
        using var resposta = await EnviarAsync(metodo, caminho, utilizador, corpo);
        var texto = await resposta.Content.ReadAsStringAsync();

        Assert.True(esperado == resposta.StatusCode, $"{metodo} {caminho}: {(int)resposta.StatusCode} {texto}");

        using var documento = JsonDocument.Parse(texto);
        return documento.RootElement.Clone();
    }

    private Task<JsonElement> CriarViagemAsync() => JsonAsync(
        HttpStatusCode.Created,
        HttpMethod.Post,
        "/api/v1/viagens",
        condutor.Id,
        new { origem = "Felgueiras", destino = "Porto", partidaEm = DateTime.UtcNow.AddDays(2), lugares = 3 });

    private async Task<string> AceitarPassageiroAsync(string viagemId)
    {
        var pedido = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/pedidos",
            passageiro.Id,
            new { viagemId, recolha = "Paços de Ferreira", destino = (string?)null });

        var participacaoId = pedido.GetProperty("id").GetString();

        using var aceite = await EnviarAsync(HttpMethod.Post, $"/api/v1/participacoes/{participacaoId}/aceitar", condutor.Id);
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);

        return participacaoId!;
    }

    [Fact]
    public async Task I110_63_Viagens_MantemOsCamposDasRespostas()
    {
        var criada = await CriarViagemAsync();
        AssertCampos(criada, "id", "origem", "destino", "partidaEm", "lugaresDisponiveis", "distanciaKm", "duracaoMinutos");
        var viagemId = criada.GetProperty("id").GetString();

        var detalhes = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/viagens/{viagemId}", passageiro.Id);
        AssertCampos(
            detalhes,
            "id", "origem", "destino", "partidaEm", "lugaresDisponiveis", "estado", "distanciaKm", "duracaoMinutos",
            "condutorId", "condutor", "pontosAdicionais", "estimativaCusto", "estimativaIndisponivelMotivo");

        var pesquisa = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/pesquisa/viagens", passageiro.Id);
        AssertCampos(pesquisa, "total", "pagina", "tamanho", "viagens");
        AssertCampos(
            pesquisa.GetProperty("viagens")[0],
            "id", "origem", "destino", "partidaEm", "lugaresDisponiveis", "distanciaKm", "duracaoMinutos",
            "pontosAdicionais", "condutor");

        var pedido = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/pedidos",
            passageiro.Id,
            new { viagemId, recolha = "Paços de Ferreira", destino = (string?)null });

        AssertCampos(pedido, "id", "viagemId", "passageiroId", "estado", "recolha", "destino", "criadoEm");
        Assert.Equal("Pendente", pedido.GetProperty("estado").GetString());

        using var aceite = await EnviarAsync(HttpMethod.Post, $"/api/v1/participacoes/{pedido.GetProperty("id").GetString()}/aceitar", condutor.Id);
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);

        var rota = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/rotas/viagens/{viagemId}", passageiro.Id);
        AssertCampos(
            rota,
            "viagemId", "participacoesAceites", "pontosOrdenados", "segmentos", "distanciaTotalKm",
            "duracaoTotalMinutos", "estimativaCusto");
    }

    [Fact]
    public async Task I110_64_Comunicacao_MantemOsCamposDasRespostas()
    {
        var viagemId = (await CriarViagemAsync()).GetProperty("id").GetString()!;
        await AceitarPassageiroAsync(viagemId);

        var aviso = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            $"/api/v1/viagens/{viagemId}/avisos",
            condutor.Id,
            new { conteudo = "Saio dez minutos mais tarde." });

        AssertCampos(aviso, "id", "conteudo", "criadoEm");

        var avisos = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/viagens/{viagemId}/avisos", passageiro.Id);
        AssertCampos(avisos[0], "id", "viagemId", "condutorId", "conteudo", "criadoEm");

        var mensagem = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/mensagens",
            passageiro.Id,
            new { destinatarioId = condutor.Id, conteudo = "Até amanhã." });

        string[] camposDaMensagem = ["id", "viagemId", "remetenteId", "destinatarioId", "conteudo", "enviadoEm"];
        AssertCampos(mensagem, camposDaMensagem);

        var conversa = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/mensagens/conversa/{condutor.Id}", passageiro.Id);
        AssertCampos(conversa[0], camposDaMensagem);

        var conversas = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/mensagens/conversas", passageiro.Id);
        AssertCampos(conversas[0], "outroUtilizadorId", "outroUtilizadorNome", "ultimaMensagem", "ultimaMensagemEm");

        var notificacoes = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/notificacoes", passageiro.Id);
        AssertCampos(notificacoes, "itens", "pagina", "tamanhoPagina", "total", "totalPaginas", "naoLidas");

        var notificacao = notificacoes.GetProperty("itens")[0];
        AssertCampos(notificacao, "id", "utilizadorId", "viagemId", "tipo", "titulo", "conteudo", "lidaEm", "criadaEm");
        Assert.Equal("Aviso", notificacao.GetProperty("tipo").GetString());

        var marcadas = await JsonAsync(HttpStatusCode.OK, HttpMethod.Put, "/api/v1/notificacoes/ler-todas", passageiro.Id);
        AssertCampos(marcadas, "marcadas");
    }

    [Fact]
    public async Task I110_65_Autenticacao_MantemOsCamposDasRespostas()
    {
        var reenvio = await JsonAsync(
            HttpStatusCode.Accepted,
            HttpMethod.Post,
            "/api/v1/autenticacao/reenviar-confirmacao",
            null,
            new { email = "ninguem@estg.ipp.pt" });

        AssertCampos(reenvio, "mensagem");

        var sessao = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Post,
            "/api/v1/autenticacao/login",
            null,
            new { email = condutor.Email, password = PalavraPasse });

        AssertCampos(sessao, "accessToken", "expiresIn");

        var eu = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/autenticacao/me", condutor.Id);
        AssertCampos(eu, "id");
    }

    [Fact]
    public async Task I110_66_Erros_TemSoOCampoErro()
    {
        var paginacao = await JsonAsync(HttpStatusCode.BadRequest, HttpMethod.Get, "/api/v1/notificacoes?pagina=0", condutor.Id);
        AssertCampos(paginacao, "erro");

        var mensagem = await JsonAsync(
            HttpStatusCode.BadRequest,
            HttpMethod.Post,
            "/api/v1/mensagens",
            condutor.Id,
            new { destinatarioId = condutor.Id, conteudo = "Olá" });

        AssertCampos(mensagem, "erro");

        var avaliacao = await JsonAsync(
            HttpStatusCode.BadRequest,
            HttpMethod.Post,
            "/api/v1/avaliacoes",
            condutor.Id,
            new { viagemId = Guid.NewGuid(), avaliadoId = passageiro.Id, classificacao = 9 });

        AssertCampos(avaliacao, "erro");
    }
}
