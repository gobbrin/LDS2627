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

    private static async Task<HttpStatusCode> EnviarAsync(HttpClient cliente, Guid destinatarioId, string conteudo)
    {
        using var resposta = await cliente.PostAsJsonAsync("/api/v1/mensagens", new { destinatarioId, conteudo });
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
    [InlineData(1, HttpStatusCode.Created)]
    [InlineData(1000, HttpStatusCode.Created)]
    [InlineData(1001, HttpStatusCode.BadRequest)]
    public async Task I110_93_LimiteDeCaracteres(int tamanho, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, await EnviarAsync(_clienteAna, _bruno.Id, new string('a', tamanho)));
    }

    /// <summary>RF-F005-02: mensagens vazias são recusadas e não ficam gravadas.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task I110_94_MensagemVazia_Devolve400SemGravar(string conteudo)
    {
        Assert.Equal(HttpStatusCode.BadRequest, await EnviarAsync(_clienteAna, _bruno.Id, conteudo));
        Assert.Equal(0, _app.NaBaseDeDados(db => db.Mensagens.Count()));
    }

    [Fact]
    public async Task I110_95_MensagemParaSiProprio_Devolve400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, await EnviarAsync(_clienteAna, _ana.Id, "Olá"));
    }

    [Fact]
    public async Task I110_96_DestinatarioInexistenteOuPorConfirmar_Devolve400()
    {
        var porConfirmar = _app.CriarUtilizador("Por confirmar", confirmado: false);

        Assert.Equal(HttpStatusCode.BadRequest, await EnviarAsync(_clienteAna, Guid.NewGuid(), "Olá"));
        Assert.Equal(HttpStatusCode.BadRequest, await EnviarAsync(_clienteAna, porConfirmar.Id, "Olá"));
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
