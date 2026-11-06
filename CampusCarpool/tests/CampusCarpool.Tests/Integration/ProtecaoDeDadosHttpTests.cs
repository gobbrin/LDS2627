using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Injeção de SQL pelos campos de texto da API (RNF-SEG-11) e dados sensíveis nos logs (RNF-MAN-05).</summary>
/// <remarks>
/// O SQL corre sobre SQLite; o que se prova é que nenhum texto recebido entra na consulta sem ser como parâmetro, e por
/// inspeção não há <c>FromSql</c> nem <c>ExecuteSql</c> em <c>src/</c>. Os logs passam pelos filtros do <c>appsettings.json</c>.
/// </remarks>
public sealed class ProtecaoDeDadosHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly Utilizador _passageiro;
    private readonly HttpClient _clienteCondutor;

    public ProtecaoDeDadosHttpTests()
    {
        _condutor = _app.CriarUtilizador("Condutor");
        _passageiro = _app.CriarUtilizador("Passageiro");
        _clienteCondutor = _app.ClienteDe(_condutor);
    }

    public void Dispose()
    {
        _clienteCondutor.Dispose();
        _app.Dispose();
    }

    private async Task<int> TotalDaPesquisaAsync(string filtro, string valor)
    {
        using var resposta = await _clienteCondutor.GetAsync(
            $"/api/v1/pesquisa/viagens?{filtro}={Uri.EscapeDataString(valor)}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("total").GetInt32();
    }

    /// <summary>RNF-SEG-11: o payload nunca devolve dados nem altera nada; nos campos livres fica gravado tal como veio.</summary>
    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE Utilizadores; --")]
    [InlineData("\" OR \"\"=\"")]
    [InlineData("x' UNION SELECT PasswordHash FROM Utilizadores --")]
    public async Task I110_109_PayloadsDeInjecao_SaoTratadosComoTexto(string payload)
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(_clienteCondutor);
        var utilizadoresAntes = _app.NaBaseDeDados(db => db.Utilizadores.Count());

        Assert.Equal(0, await TotalDaPesquisaAsync("origem", payload));
        Assert.Equal(0, await TotalDaPesquisaAsync("destino", payload));

        using var anonimo = _app.CreateClient();
        using var loginEmail = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = payload, password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.Unauthorized, loginEmail.StatusCode);

        using var loginPalavraPasse = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = _condutor.Email, password = payload });
        Assert.Equal(HttpStatusCode.Unauthorized, loginPalavraPasse.StatusCode);

        using var mensagem = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/mensagens",
            new { destinatarioId = _passageiro.Id, conteudo = payload });
        Assert.Equal(HttpStatusCode.Created, mensagem.StatusCode);

        using var aviso = await _clienteCondutor.PostAsJsonAsync(
            $"/api/v1/viagens/{viagemId}/avisos",
            new { conteudo = payload });
        Assert.Equal(HttpStatusCode.Created, aviso.StatusCode);

        using var viagem = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/viagens",
            new { origem = payload, destino = "Braga", partidaEm = DateTime.UtcNow.AddDays(1), lugares = 2 });
        Assert.Equal(HttpStatusCode.Created, viagem.StatusCode);

        Assert.Equal(utilizadoresAntes, _app.NaBaseDeDados(db => db.Utilizadores.Count()));
        Assert.Equal(payload, _app.NaBaseDeDados(db => db.Mensagens.AsNoTracking().Single().Conteudo));
        Assert.Equal(payload, _app.NaBaseDeDados(db => db.Avisos.AsNoTracking().Single().Conteudo));
        Assert.Equal(2, _app.NaBaseDeDados(db => db.Viagens.Count()));
    }

    /// <summary>Os caracteres especiais de LIKE são texto: <c>%</c> não pode devolver todas as viagens.</summary>
    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    public async Task I110_110_CaracteresEspeciaisDaPesquisa_SaoTexto(string filtro)
    {
        await AplicacaoReal.CriarViagemAsync(_clienteCondutor);

        Assert.Equal(1, await TotalDaPesquisaAsync("origem", "Porto"));
        Assert.Equal(0, await TotalDaPesquisaAsync("origem", filtro));
    }

    /// <summary>RNF-MAN-05: um percurso com credenciais, tokens e uma mensagem, incluindo um login falhado, não deixa nada disso nos logs.</summary>
    [Fact]
    public async Task I110_111_Logs_NaoContemPalavrasPasseTokensNemMensagens()
    {
        const string textoDaMensagem = "texto-privado-da-mensagem";
        const string palavraPasseErrada = "Palavra-passe-errada-9";
        using var anonimo = _app.CreateClient();

        using var registo = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/registo",
            new { nome = "Novo", email = "novo@estg.ipp.pt", password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.Accepted, registo.StatusCode);

        using var falhado = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = _condutor.Email, password = palavraPasseErrada });
        Assert.Equal(HttpStatusCode.Unauthorized, falhado.StatusCode);

        using var login = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = _condutor.Email, password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString()!;

        using var mensagem = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/mensagens",
            new { destinatarioId = _passageiro.Id, conteudo = textoDaMensagem });
        Assert.Equal(HttpStatusCode.Created, mensagem.StatusCode);

        var logs = string.Join('\n', _app.Logs);

        // Sem isto, um filtro errado faria o teste passar sem ver nada.
        Assert.NotEmpty(_app.Logs);
        Assert.DoesNotContain(AplicacaoReal.PalavraPasse, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(palavraPasseErrada, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(Assert.Single(_app.Email.Tokens), logs, StringComparison.Ordinal);
        Assert.DoesNotContain(textoDaMensagem, logs, StringComparison.Ordinal);
    }
}
