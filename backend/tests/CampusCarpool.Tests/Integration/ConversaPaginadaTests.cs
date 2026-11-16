using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Conversa paginada por cursor (BE-13, I112): <c>GET /mensagens/conversa/{id}?antesDe={mensagemId}&amp;tamanho=n</c>,
/// pela API real.
/// </summary>
public sealed class ConversaPaginadaTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _ana;
    private readonly Utilizador _rui;
    private readonly HttpClient _cliente;

    // Ids das mensagens entre a Ana e o Rui, por ordem cronológica.
    private readonly List<Guid> _ids = [];

    public ConversaPaginadaTests()
    {
        _ana = _app.CriarUtilizador("Ana");
        _rui = _app.CriarUtilizador("Rui");
        _cliente = _app.ClienteDe(_ana);

        var inicio = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

        // Cinco mensagens, um minuto entre cada, alternando o sentido.
        for (int i = 0; i < 5; i++)
        {
            Guid remetente = i % 2 == 0 ? _ana.Id : _rui.Id;
            Guid destinatario = i % 2 == 0 ? _rui.Id : _ana.Id;
            _ids.Add(GravarMensagem(remetente, destinatario, $"m{i}", inicio.AddMinutes(i)));
        }
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _app.Dispose();
    }

    private Guid GravarMensagem(Guid remetente, Guid destinatario, string conteudo, DateTime enviadoEm, Guid? id = null)
    {
        var mensagem = new Mensagem
        {
            RemetenteId = remetente,
            DestinatarioId = destinatario,
            Conteudo = conteudo,
            EnviadoEm = enviadoEm
        };

        if (id is Guid escolhido)
            mensagem.Id = escolhido;

        return _app.NaBaseDeDados(db =>
        {
            db.Mensagens.Add(mensagem);
            db.SaveChanges();
            return mensagem.Id;
        });
    }

    private async Task<List<Guid>> PaginaAsync(string consulta)
    {
        using HttpResponseMessage resposta = await _cliente.GetAsync($"/api/v1/mensagens/conversa/{_rui.Id}{consulta}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        List<MensagemIndividual> mensagens = (await resposta.Content.ReadFromJsonAsync<List<MensagemIndividual>>())!;
        return mensagens.Select(m => m.Id).ToList();
    }

    [Fact]
    public async Task I112_29_SemParametros_DevolveAsMaisRecentesPorOrdemCronologica()
    {
        Assert.Equal(_ids, await PaginaAsync(""));
    }

    [Fact]
    public async Task I112_30_ComCursor_DevolveAsAnterioresAteAoFim()
    {
        // Primeira página: as duas mais recentes.
        Assert.Equal([_ids[3], _ids[4]], await PaginaAsync("?tamanho=2"));

        // Anteriores à mais antiga já mostrada.
        Assert.Equal([_ids[1], _ids[2]], await PaginaAsync($"?antesDe={_ids[3]}&tamanho=2"));

        // Menos do que o tamanho: não há mais antigas.
        Assert.Equal([_ids[0]], await PaginaAsync($"?antesDe={_ids[1]}&tamanho=2"));
        Assert.Empty(await PaginaAsync($"?antesDe={_ids[0]}&tamanho=2"));
    }

    [Fact]
    public async Task I112_31_MensagensNoMesmoInstante_NemSeRepetemNemSePerdem()
    {
        var instante = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        for (int i = 1; i <= 3; i++)
            GravarMensagem(_ana.Id, _rui.Id, $"igual{i}", instante, Guid.Parse($"00000000-0000-0000-0000-00000000000{i}"));

        List<Guid> todas = await PaginaAsync("");
        Assert.Equal(8, todas.Count);

        // Percorre a conversa uma mensagem de cada vez, do fim para o início.
        var percorridas = new List<Guid>();
        List<Guid> pagina = await PaginaAsync("?tamanho=1");

        while (pagina.Count > 0)
        {
            percorridas.Insert(0, pagina[0]);
            pagina = await PaginaAsync($"?antesDe={pagina[0]}&tamanho=1");
        }

        Assert.Equal(todas, percorridas);
    }

    [Theory]
    [InlineData("?tamanho=0")]
    [InlineData("?tamanho=101")]
    public async Task I112_32_TamanhoForaDe1A100_Devolve400(string consulta)
    {
        using HttpResponseMessage resposta = await _cliente.GetAsync($"/api/v1/mensagens/conversa/{_rui.Id}{consulta}");

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "O tamanho da página tem de estar entre 1 e 100.");
    }

    [Fact]
    public async Task I112_33_CursorDeOutraConversa_Devolve400()
    {
        Utilizador outro = _app.CriarUtilizador("Outro");
        Guid alheia = GravarMensagem(_rui.Id, outro.Id, "alheia", DateTime.UtcNow);

        using HttpResponseMessage resposta = await _cliente.GetAsync($"/api/v1/mensagens/conversa/{_rui.Id}?antesDe={alheia}");

        await RespostasDeErro.AssertErroAsync(
            resposta, HttpStatusCode.BadRequest, "A mensagem indicada em antesDe não pertence a esta conversa.");
    }

    [Fact]
    public async Task I112_34_ListaDeConversas_TrazOIdDaUltimaMensagem()
    {
        using HttpResponseMessage resposta = await _cliente.GetAsync("/api/v1/mensagens/conversas");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        List<ResumoConversa> conversas = (await resposta.Content.ReadFromJsonAsync<List<ResumoConversa>>())!;

        ResumoConversa conversa = Assert.Single(conversas);
        Assert.Equal(_ids[4], conversa.UltimaMensagemId);
        Assert.Equal("m4", conversa.UltimaMensagem);
    }
}
