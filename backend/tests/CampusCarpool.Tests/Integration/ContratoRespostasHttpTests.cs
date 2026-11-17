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
                services.AddDbContext<CampusCarpoolDbContext>(options => options.UseBaseDeTeste(ligacao));

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

        db.Database.CriarEsquemaDeTeste();
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
            "duracaoTotalMinutos", "estimativaCusto", "geometria", "geometriaAproximada");

        // I112 (BE-06): DTOs próprios; o ponto não leva a participação e o troço refere os pontos pela ordem.
        AssertCampos(rota.GetProperty("pontosOrdenados")[0], "ordem", "tipo", "local", "latitude", "longitude");
        AssertCampos(rota.GetProperty("segmentos")[0], "deOrdem", "ateOrdem", "distanciaKm", "duracaoMinutos");
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

        string[] camposDaMensagem = ["id", "remetenteId", "destinatarioId", "conteudo", "enviadoEm"];
        AssertCampos(mensagem, camposDaMensagem);

        var conversa = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/mensagens/conversa/{condutor.Id}", passageiro.Id);
        AssertCampos(conversa[0], camposDaMensagem);

        var conversas = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/mensagens/conversas", passageiro.Id);
        // D-4: página com { total, pagina, tamanho, conversas }.
        AssertCampos(conversas, "total", "pagina", "tamanho", "conversas");
        AssertCampos(
            conversas.GetProperty("conversas")[0],
            "outroUtilizadorId", "outroUtilizadorNome", "ultimaMensagemId", "ultimaMensagem", "ultimaMensagemEm");

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
    /// <summary>
    /// WEB-01/MOB-01: a simulação da alteração responde com os mesmos campos de <c>GET /rotas/viagens/{id}</c>, para os
    /// frontends usarem o mesmo modelo e o mesmo mapa na rota guardada e na simulada.
    /// </summary>
    public async Task I112_07_SimularAlteracao_TemOsCamposDaRotaDaViagem()
    {
        var viagemId = (await CriarViagemAsync()).GetProperty("id").GetString()!;
        await AceitarPassageiroAsync(viagemId);

        var simulada = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Post,
            $"/api/v1/viagens/{viagemId}/simular-alteracao",
            condutor.Id,
            new { origem = "Felgueiras", destino = "Braga" });

        AssertCampos(
            simulada,
            "viagemId", "participacoesAceites", "pontosOrdenados", "segmentos", "distanciaTotalKm",
            "duracaoTotalMinutos", "estimativaCusto", "geometria", "geometriaAproximada");
        AssertCampos(simulada.GetProperty("pontosOrdenados")[0], "ordem", "tipo", "local", "latitude", "longitude");
        AssertCampos(simulada.GetProperty("segmentos")[0], "deOrdem", "ateOrdem", "distanciaKm", "duracaoMinutos");
        AssertCampos(
            simulada.GetProperty("estimativaCusto"),
            "distanciaKm", "precoPorKm", "passageirosAceites", "totalParticipantes", "custoTotalEstimado",
            "custoPorParticipante", "indicativo", "moeda");

        // A linha vem em pares [latitude, longitude] e, sem a HERE, são retas entre os pontos.
        Assert.True(simulada.GetProperty("geometriaAproximada").GetBoolean());
        Assert.Equal(2, simulada.GetProperty("geometria")[0].GetArrayLength());
    }

    [Fact]
    /// <summary>DEF-BE-054: a pré-visualização da rota traz a linha do mapa em pares [latitude, longitude].</summary>
    public async Task I112_01_RotaCalculada_TemAGeometriaEmPares()
    {
        var rota = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Post,
            "/api/v1/rotas/calcular",
            condutor.Id,
            new { origem = "Felgueiras", destino = "Porto" });

        AssertCampos(rota, "distanciaKm", "duracaoMinutos", "origem", "destino", "geometria", "geometriaAproximada");

        // Sem Here:ApiKey nos testes: a reserva é uma reta da origem ao destino.
        Assert.True(rota.GetProperty("geometriaAproximada").GetBoolean());
        var geometria = rota.GetProperty("geometria").EnumerateArray().ToList();
        Assert.Equal(2, geometria.Count);
        Assert.Equal(rota.GetProperty("origem").GetProperty("latitude").GetDecimal(), geometria[0][0].GetDecimal());
        Assert.Equal(rota.GetProperty("origem").GetProperty("longitude").GetDecimal(), geometria[0][1].GetDecimal());
        Assert.Equal(2, geometria[1].GetArrayLength());
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20%20")]
    [InlineData(null)]
    /// <summary>E5-a: texto vazio, só com espaços ou em falta em <c>GET /rotas/geocodificar</c> dá a mensagem do controller.</summary>
    public async Task I112_112_Geocodificar_TextoVazioOuEmFalta_DaAMensagemDoController(string? texto)
    {
        string caminho = texto is null ? "/api/v1/rotas/geocodificar" : $"/api/v1/rotas/geocodificar?texto={texto}";

        var erro = await JsonAsync(HttpStatusCode.BadRequest, HttpMethod.Get, caminho, condutor.Id);

        Assert.Equal("Indica o local a procurar.", erro.GetProperty("erro").GetString());
    }

    [Fact]
    /// <summary>E5-b: <c>POST /rotas/calcular</c> recusa a origem igual ao destino (mesmo texto, sem distinguir maiúsculas), como <c>POST /viagens</c>.</summary>
    public async Task I112_113_Calcular_OrigemIgualAoDestino_400()
    {
        var erro = await JsonAsync(
            HttpStatusCode.BadRequest,
            HttpMethod.Post,
            "/api/v1/rotas/calcular",
            condutor.Id,
            new { origem = "Porto", destino = " porto " });

        Assert.Equal("A origem e o destino não podem ser o mesmo local.", erro.GetProperty("erro").GetString());
    }

    // Viagem já concluída em que o passageiro participou: só assim se pode avaliar (RF-F007-01).
    private Guid CriarViagemConcluidaComPassageiro()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>();

        var viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            OrigemLatitude = 41.15m,
            OrigemLongitude = -8.61m,
            DestinoLatitude = 41.55m,
            DestinoLongitude = -8.42m,
            PartidaEm = DateTime.UtcNow.AddDays(-2),
            Estado = Domain.Enums.EstadoViagem.Concluida,
            LugaresOferecidos = 3,
            LugaresDisponiveis = 2
        };

        db.Viagens.Add(viagem);
        db.Participacoes.Add(new Participacao
        {
            Destino = viagem.Destino,
            DestinoLatitude = viagem.DestinoLatitude,
            DestinoLongitude = viagem.DestinoLongitude,
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = Domain.Enums.EstadoParticipacao.Aceite,
            AceiteEm = DateTime.UtcNow.AddDays(-3)
        });
        db.SaveChanges();

        return viagem.Id;
    }

    [Fact]
    /// <summary>BE-10: perfil próprio, perfil público e ligações externas.</summary>
    public async Task I112_02_Utilizadores_MantemOsCamposDasRespostas()
    {
        var ligacao = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/utilizadores/me/ligacoes",
            condutor.Id,
            new { url = "https://github.com/exemplo", descricao = "GitHub" });
        AssertCampos(ligacao, "id", "url", "descricao");

        var perfil = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/utilizadores/me", condutor.Id);
        AssertCampos(
            perfil,
            "id", "nome", "email", "emailConfirmado", "curso", "instituicao", "criadoEm", "ligacoesExternas",
            "classificacaoMedia", "totalAvaliacoes");
        AssertCampos(perfil.GetProperty("ligacoesExternas")[0], "id", "url", "descricao");

        var alterado = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Put,
            "/api/v1/utilizadores/me",
            condutor.Id,
            new { nome = "Condutor Alterado", curso = "LEI", instituicao = "ESTG" });
        AssertCampos(alterado, "id", "nome", "email", "emailConfirmado", "curso", "instituicao");

        var publico = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/utilizadores/{condutor.Id}", passageiro.Id);
        AssertCampos(
            publico, "id", "nome", "curso", "instituicao", "ligacoesExternas", "classificacaoMedia", "totalAvaliacoes");
    }

    [Fact]
    /// <summary>BE-10: listas do condutor (viagens, pedidos, passageiros) e simulação.</summary>
    public async Task I112_03_ViagensDoCondutor_MantemOsCamposDasRespostas()
    {
        var viagemId = (await CriarViagemAsync()).GetProperty("id").GetString()!;

        var minhas = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/viagens/minhas", condutor.Id);
        // D-4: página com { total, pagina, tamanho, viagens }, como a pesquisa.
        AssertCampos(minhas, "total", "pagina", "tamanho", "viagens");
        AssertCampos(
            minhas.GetProperty("viagens")[0],
            "id", "origem", "destino", "partidaEm", "lugaresOferecidos", "lugaresDisponiveis");

        var pedido = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/pedidos",
            passageiro.Id,
            new { viagemId, recolha = "Paços de Ferreira", destino = (string?)null });
        var pedidoId = pedido.GetProperty("id").GetString();

        var pedidos = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/viagens/{viagemId}/pedidos", condutor.Id);
        AssertCampos(pedidos[0], "id", "passageiroId", "nome", "recolha", "destino", "criadoEm");

        var simulacao = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Post,
            $"/api/v1/viagens/{viagemId}/simulacao",
            condutor.Id,
            new { pedidoId });
        AssertCampos(
            simulacao,
            "viagemId", "pedidoId", "origemViagem", "destinoViagem", "recolhaPassageiro", "destinoPassageiro",
            "distanciaOriginalKm", "duracaoOriginalMinutos", "distanciaSimuladaKm", "duracaoSimuladaMinutos",
            "desvioDistanciaKm", "desvioDuracaoMinutos", "ordemOriginal", "ordemSimulada", "rotaSimulada");

        // BE-35 / D-2: a rota simulada tem o formato de GET /rotas/viagens/{id}, com coordenadas e linha.
        AssertCampos(
            simulacao.GetProperty("rotaSimulada"),
            "viagemId", "participacoesAceites", "pontosOrdenados", "segmentos", "distanciaTotalKm",
            "duracaoTotalMinutos", "estimativaCusto", "geometria", "geometriaAproximada");
        AssertCampos(simulacao.GetProperty("rotaSimulada").GetProperty("pontosOrdenados")[1], "ordem", "tipo", "local", "latitude", "longitude");
        Assert.Equal("Recolha", simulacao.GetProperty("rotaSimulada").GetProperty("pontosOrdenados")[1].GetProperty("tipo").GetString());

        using var aceite = await EnviarAsync(HttpMethod.Post, $"/api/v1/participacoes/{pedidoId}/aceitar", condutor.Id);
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);

        var passageiros = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/viagens/{viagemId}/passageiros", condutor.Id);
        AssertCampos(passageiros[0], "id", "passageiroId", "nome", "recolha", "destino");
    }

    [Fact]
    /// <summary>BE-10: geocodificação e histórico.</summary>
    public async Task I112_04_GeocodificacaoEHistorico_MantemOsCamposDasRespostas()
    {
        var coordenadas = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/rotas/geocodificar?texto=Porto", condutor.Id);
        AssertCampos(coordenadas, "latitude", "longitude");

        CriarViagemConcluidaComPassageiro();

        var historico = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, "/api/v1/historico", passageiro.Id);
        // D-4: as seis listas da página, com a página, o tamanho e o total de cada lista.
        AssertCampos(
            historico,
            "pagina", "tamanho", "totais",
            "viagensComoCondutor", "viagensComoPassageiro", "pedidosEfetuados", "pedidosRecebidos", "notificacoes", "avisos");
        AssertCampos(
            historico.GetProperty("totais"),
            "viagensComoCondutor", "viagensComoPassageiro", "pedidosEfetuados", "pedidosRecebidos", "notificacoes", "avisos");
        AssertCampos(
            historico.GetProperty("pedidosEfetuados")[0],
            "id", "viagemId", "origem", "destino", "partidaEm", "criadoEm", "estado", "pontoRecolha", "pontoDestino");
        AssertCampos(
            historico.GetProperty("viagensComoPassageiro")[0],
            "id", "origem", "destino", "partidaEm", "estado", "lugaresOferecidos", "participacaoId", "estadoParticipacao");
    }

    [Fact]
    /// <summary>BE-10: criar, alterar e consultar avaliações.</summary>
    public async Task I112_05_Avaliacoes_MantemOsCamposDasRespostas()
    {
        var viagemId = CriarViagemConcluidaComPassageiro();

        var criada = await JsonAsync(
            HttpStatusCode.Created,
            HttpMethod.Post,
            "/api/v1/avaliacoes",
            passageiro.Id,
            new { viagemId, avaliadoId = condutor.Id, classificacao = 4 });
        AssertCampos(criada, "classificacao", "data");

        var alterada = await JsonAsync(
            HttpStatusCode.OK,
            HttpMethod.Put,
            $"/api/v1/avaliacoes/utilizadores/{condutor.Id}",
            passageiro.Id,
            new { classificacao = 5 });
        AssertCampos(alterada, "classificacao", "data");

        var minha = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/avaliacoes/utilizadores/{condutor.Id}/minha", passageiro.Id);
        AssertCampos(minha, "classificacao", "data");

        var consulta = await JsonAsync(HttpStatusCode.OK, HttpMethod.Get, $"/api/v1/avaliacoes/utilizadores/{condutor.Id}", passageiro.Id);
        // BE-43: só a média e o contador, com os nomes do perfil; sem lista nem autor.
        AssertCampos(consulta, "classificacaoMedia", "totalAvaliacoes");
    }

    [Fact]
    /// <summary>BE-10: os erros previstos de 403, 404 e 409 (via <c>Resultado</c>) trazem só <c>{ erro }</c>.</summary>
    /// <remarks>
    /// Os 409 de concorrência, 500, 404 de rota, 405 e 415 também saem em <c>{ erro }</c> (BE-08, ver
    /// <c>PipelineHttpTests</c>); o 401 não tem corpo (exceção documentada no contrato).
    /// </remarks>
    public async Task I112_06_ErrosPrevistos_TemSoOCampoErro()
    {
        var naoEncontrada = await JsonAsync(HttpStatusCode.NotFound, HttpMethod.Get, $"/api/v1/viagens/{Guid.NewGuid()}", condutor.Id);
        AssertCampos(naoEncontrada, "erro");

        var viagemId = (await CriarViagemAsync()).GetProperty("id").GetString()!;

        var proibido = await JsonAsync(HttpStatusCode.Forbidden, HttpMethod.Get, $"/api/v1/viagens/{viagemId}/pedidos", passageiro.Id);
        AssertCampos(proibido, "erro");

        var corpoDoPedido = new { viagemId, recolha = "Paços de Ferreira", destino = (string?)null };
        await JsonAsync(HttpStatusCode.Created, HttpMethod.Post, "/api/v1/pedidos", passageiro.Id, corpoDoPedido);

        var duplicado = await JsonAsync(HttpStatusCode.Conflict, HttpMethod.Post, "/api/v1/pedidos", passageiro.Id, corpoDoPedido);
        AssertCampos(duplicado, "erro");
        Assert.Equal("Já existe um pedido ou participação ativa nesta viagem.", duplicado.GetProperty("erro").GetString());
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
