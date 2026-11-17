using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Regras das mensagens com o <c>MensagemService</c> real, do pedido HTTP à BD (TE-06, RF-F005-02, RF-F005-04, P-14).</summary>
/// <remarks>Os <c>MensagensControllerTests</c> usam um serviço falso e só provam a tradução dos erros.</remarks>
public sealed class MensagensServicoRealHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _ana;
    private readonly Utilizador _bruno;
    private readonly HttpClient _clienteAna;

    public MensagensServicoRealHttpTests()
    {
        _ana = _app.CriarUtilizador("Ana");
        _bruno = _app.CriarUtilizador("Bruno");
        _clienteAna = _app.ClienteDe(_ana);
    }

    public void Dispose()
    {
        _clienteAna.Dispose();
        _app.Dispose();
    }

    // Envia uma mensagem e devolve a resposta inteira, para os testes de erro lerem o corpo { erro } (BE-09).
    private static Task<HttpResponseMessage> EnviarComRespostaAsync(HttpClient cliente, Guid destinatarioId, string conteudo) =>
        cliente.PostAsJsonAsync("/api/v1/mensagens", new { destinatarioId, conteudo });

    private static async Task<HttpStatusCode> EnviarAsync(HttpClient cliente, Guid destinatarioId, string conteudo)
    {
        using var resposta = await EnviarComRespostaAsync(cliente, destinatarioId, conteudo);
        return resposta.StatusCode;
    }

    private async Task<List<string>> ConteudosDaConversaAsync(Utilizador leitor, Guid outroId)
    {
        using var cliente = _app.ClienteDe(leitor);
        using var resposta = await cliente.GetAsync($"/api/v1/mensagens/conversa/{outroId}");
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return [.. json.RootElement.EnumerateArray().Select(m => m.GetProperty("conteudo").GetString()!)];
    }

    /// <summary>RF-F005-02: o limite de P-14 é exatamente 1000 caracteres.</summary>
    [Theory]
    [InlineData(1, HttpStatusCode.Created, null)]
    [InlineData(1000, HttpStatusCode.Created, null)]
    [InlineData(1001, HttpStatusCode.BadRequest, "A mensagem deve conter entre 1 e 1000 caracteres.")]
    public async Task I110_93_LimiteDeCaracteres(int tamanho, HttpStatusCode esperado, string? erro)
    {
        using var resposta = await EnviarComRespostaAsync(_clienteAna, _bruno.Id, new string('a', tamanho));

        // Aceite: basta o código. Recusada: o código e a mensagem do corpo { erro }.
        if (erro is null)
            Assert.Equal(esperado, resposta.StatusCode);
        else
            await RespostasDeErro.AssertErroAsync(resposta, esperado, erro);
    }

    /// <summary>RF-F005-02: mensagens vazias são recusadas e não ficam gravadas.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task I110_94_MensagemVazia_Devolve400SemGravar(string conteudo)
    {
        using var resposta = await EnviarComRespostaAsync(_clienteAna, _bruno.Id, conteudo);

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "A mensagem deve conter entre 1 e 1000 caracteres.");
        Assert.Equal(0, _app.NaBaseDeDados(db => db.Mensagens.Count()));
    }

    [Fact]
    public async Task I110_95_MensagemParaSiProprio_Devolve400()
    {
        using var resposta = await EnviarComRespostaAsync(_clienteAna, _ana.Id, "Olá");

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "Não é possível enviar mensagens para si próprio.");
    }

    [Fact]
    public async Task I110_96_DestinatarioInexistenteOuPorConfirmar_Devolve400()
    {
        var porConfirmar = _app.CriarUtilizador("Por confirmar", confirmado: false);
        const string erro = "O destinatário não existe ou não tem a conta confirmada.";

        using var inexistente = await EnviarComRespostaAsync(_clienteAna, Guid.NewGuid(), "Olá");
        using var naoConfirmado = await EnviarComRespostaAsync(_clienteAna, porConfirmar.Id, "Olá");

        await RespostasDeErro.AssertErroAsync(inexistente, HttpStatusCode.BadRequest, erro);
        await RespostasDeErro.AssertErroAsync(naoConfirmado, HttpStatusCode.BadRequest, erro);
    }

    /// <summary>RF-F005-04: cada conversa só tem as mensagens dos seus dois intervenientes.</summary>
    [Fact]
    public async Task I110_97_Conversa_SoMostraMensagensDosDoisIntervenientes()
    {
        var carla = _app.CriarUtilizador("Carla");
        using var clienteCarla = _app.ClienteDe(carla);
        await EnviarAsync(_clienteAna, _bruno.Id, "Ana para Bruno");
        await EnviarAsync(clienteCarla, _bruno.Id, "Carla para Bruno");

        Assert.Equal(["Ana para Bruno"], await ConteudosDaConversaAsync(_ana, _bruno.Id));
        Assert.Equal(["Carla para Bruno"], await ConteudosDaConversaAsync(carla, _bruno.Id));
        Assert.Empty(await ConteudosDaConversaAsync(carla, _ana.Id));
    }

    /// <summary>RF-F005-04: a conversa é a mesma vista por cada um dos dois lados.</summary>
    [Fact]
    public async Task I110_98_Conversa_EAMesmaParaOsDoisIntervenientes()
    {
        await EnviarAsync(_clienteAna, _bruno.Id, "Olá, Bruno");

        Assert.Equal(["Olá, Bruno"], await ConteudosDaConversaAsync(_bruno, _ana.Id));
    }
}
