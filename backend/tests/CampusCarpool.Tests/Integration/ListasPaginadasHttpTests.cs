using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Listas paginadas da D-4 (<c>/historico</c>, <c>/viagens/minhas</c>, <c>/mensagens/conversas</c>) e a lista de
/// passageiros sem os ids das participações alheias (BE-37), pelo pipeline HTTP real (<see cref="AplicacaoReal"/>).
/// </summary>
/// <remarks>Cada teste lê o corpo JSON: os elementos da página, a página, o tamanho e o total.</remarks>
public sealed class ListasPaginadasHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly HttpClient _clienteCondutor;

    public ListasPaginadasHttpTests()
    {
        _condutor = _app.CriarUtilizador("Condutor");
        _clienteCondutor = _app.ClienteDe(_condutor);
    }

    public void Dispose()
    {
        _clienteCondutor.Dispose();
        _app.Dispose();
    }

    private static async Task<JsonDocument> JsonAsync(HttpClient cliente, string caminho)
    {
        using HttpResponseMessage resposta = await cliente.GetAsync(caminho);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    /// <summary>D-4: com 3 viagens ativas e páginas de 2, a página 2 traz a terceira e o total 3.</summary>
    public async Task I112_74_ViagensDoCondutor_SegundaPagina()
    {
        for (int i = 0; i < 3; i++)
            await AplicacaoReal.CriarViagemAsync(_clienteCondutor);

        using JsonDocument json = await JsonAsync(_clienteCondutor, "/api/v1/viagens/minhas?pagina=2&tamanho=2");
        JsonElement raiz = json.RootElement;

        Assert.Equal(3, raiz.GetProperty("total").GetInt32());
        Assert.Equal(2, raiz.GetProperty("pagina").GetInt32());
        Assert.Equal(2, raiz.GetProperty("tamanho").GetInt32());
        Assert.Equal(1, raiz.GetProperty("viagens").GetArrayLength());
    }

    [Fact]
    /// <summary>D-4: o histórico aplica a página a cada lista e traz o total de cada uma em <c>totais</c>.</summary>
    public async Task I112_75_Historico_SegundaPaginaComOsTotais()
    {
        for (int i = 0; i < 3; i++)
            await AplicacaoReal.CriarViagemAsync(_clienteCondutor);

        using JsonDocument json = await JsonAsync(_clienteCondutor, "/api/v1/historico?pagina=2&tamanho=2");
        JsonElement raiz = json.RootElement;

        Assert.Equal(2, raiz.GetProperty("pagina").GetInt32());
        Assert.Equal(1, raiz.GetProperty("viagensComoCondutor").GetArrayLength());
        Assert.Equal(3, raiz.GetProperty("totais").GetProperty("viagensComoCondutor").GetInt32());
        Assert.Equal(0, raiz.GetProperty("totais").GetProperty("pedidosEfetuados").GetInt32());
        Assert.Equal(0, raiz.GetProperty("viagensComoPassageiro").GetArrayLength());
    }

    [Fact]
    /// <summary>D-4: duas conversas e páginas de 1: a primeira página traz a mais recente e o total 2.</summary>
    public async Task I112_76_Conversas_PrimeiraPaginaTrazAMaisRecente()
    {
        Utilizador ana = _app.CriarUtilizador("Ana");
        Utilizador rui = _app.CriarUtilizador("Rui");

        using (var primeira = await _clienteCondutor.PostAsJsonAsync("/api/v1/mensagens", new { destinatarioId = ana.Id, conteudo = "Olá Ana" }))
            Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        using (var segunda = await _clienteCondutor.PostAsJsonAsync("/api/v1/mensagens", new { destinatarioId = rui.Id, conteudo = "Olá Rui" }))
            Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        using JsonDocument json = await JsonAsync(_clienteCondutor, "/api/v1/mensagens/conversas?pagina=1&tamanho=1");
        JsonElement raiz = json.RootElement;

        Assert.Equal(2, raiz.GetProperty("total").GetInt32());
        JsonElement conversa = Assert.Single(raiz.GetProperty("conversas").EnumerateArray());
        Assert.Equal("Rui", conversa.GetProperty("outroUtilizadorNome").GetString());
    }

    [Theory]
    [InlineData("/api/v1/historico?tamanho=101", "O tamanho da página tem de estar entre 1 e 100.")]
    [InlineData("/api/v1/viagens/minhas?pagina=0", "A página começa em 1.")]
    [InlineData("/api/v1/mensagens/conversas?tamanho=0", "O tamanho da página tem de estar entre 1 e 100.")]
    /// <summary>BE-41 / D-4: a paginação inválida dá 400 com uma mensagem por regra, a mesma em todas as listas.</summary>
    public async Task I112_77_PaginacaoInvalida_Devolve400ComAMensagemDaRegra(string caminho, string mensagem)
    {
        using HttpResponseMessage resposta = await _clienteCondutor.GetAsync(caminho);

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, mensagem);
    }

    [Fact]
    /// <summary>
    /// BE-37 / D-1: um passageiro aceite vê os outros aceites e os pontos deles, mas não os ids das participações;
    /// o condutor recebe os ids.
    /// </summary>
    public async Task I112_78_Passageiros_SoOCondutorRecebeOsIdsDasParticipacoes()
    {
        Utilizador ana = _app.CriarUtilizador("Ana");
        Utilizador rui = _app.CriarUtilizador("Rui");
        using HttpClient clienteAna = _app.ClienteDe(ana);
        using HttpClient clienteRui = _app.ClienteDe(rui);
        Guid viagemId = await AplicacaoReal.CriarViagemAsync(_clienteCondutor);
        await AplicacaoReal.AceitarAsync(_clienteCondutor, await AplicacaoReal.PedirParticipacaoAsync(clienteAna, viagemId, "Maia"));
        await AplicacaoReal.AceitarAsync(_clienteCondutor, await AplicacaoReal.PedirParticipacaoAsync(clienteRui, viagemId, "Trofa"));

        using JsonDocument vistaDoRui = await JsonAsync(clienteRui, $"/api/v1/viagens/{viagemId}/passageiros");
        using JsonDocument vistaDoCondutor = await JsonAsync(_clienteCondutor, $"/api/v1/viagens/{viagemId}/passageiros");

        JsonElement anaVistaPeloRui = vistaDoRui.RootElement.EnumerateArray().Single(p => p.GetProperty("nome").GetString() == "Ana");
        Assert.Equal(JsonValueKind.Null, anaVistaPeloRui.GetProperty("id").ValueKind);
        Assert.Equal("Maia", anaVistaPeloRui.GetProperty("recolha").GetString());
        Assert.Equal(ana.Id, anaVistaPeloRui.GetProperty("passageiroId").GetGuid());

        foreach (JsonElement passageiro in vistaDoCondutor.RootElement.EnumerateArray())
            Assert.NotEqual(Guid.Empty, passageiro.GetProperty("id").GetGuid());
    }
}
