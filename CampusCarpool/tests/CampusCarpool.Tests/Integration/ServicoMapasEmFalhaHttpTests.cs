using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Comportamento com o Serviço de Mapas em baixo ou sem resposta (RNF-FIA-02, RF-F008-12, P-19).</summary>
public sealed class ServicoMapasEmFalhaHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly Utilizador _passageiro;
    private readonly HttpClient _clienteCondutor;
    private readonly HttpClient _clientePassageiro;

    public ServicoMapasEmFalhaHttpTests()
    {
        _condutor = _app.CriarUtilizador("Condutor");
        _passageiro = _app.CriarUtilizador("Passageiro");
        _clienteCondutor = _app.ClienteDe(_condutor);
        _clientePassageiro = _app.ClienteDe(_passageiro);
    }

    public void Dispose()
    {
        _clienteCondutor.Dispose();
        _clientePassageiro.Dispose();
        _app.Dispose();
    }

    private void DesligarMapas(bool semResposta)
    {
        _app.Mapas.SimularFalha = !semResposta;
        _app.Mapas.SimularTempoEsgotado = semResposta;
    }

    private async Task<Guid> ViagemComPassageiroAceiteAsync()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(_clienteCondutor);
        await AplicacaoReal.AceitarAsync(
            _clienteCondutor,
            await AplicacaoReal.PedirParticipacaoAsync(_clientePassageiro, viagemId));

        return viagemId;
    }

    /// <summary>RNF-FIA-02: registo, login, mensagens, avisos, notificações e histórico continuam a funcionar.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task I110_106_FuncionalidadesSemMapas_ContinuamDisponiveis(bool semResposta)
    {
        var viagemId = await ViagemComPassageiroAceiteAsync();
        DesligarMapas(semResposta);
        using var anonimo = _app.CreateClient();

        using var registo = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/registo",
            new { nome = "Novo", email = "novo@estg.ipp.pt", password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.Accepted, registo.StatusCode);

        using var login = await anonimo.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = _condutor.Email, password = AplicacaoReal.PalavraPasse });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var mensagem = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/mensagens",
            new { destinatarioId = _passageiro.Id, conteudo = "Olá" });
        Assert.Equal(HttpStatusCode.Created, mensagem.StatusCode);

        using var aviso = await _clienteCondutor.PostAsJsonAsync(
            $"/api/v1/viagens/{viagemId}/avisos",
            new { conteudo = "Atraso de 5 minutos." });
        Assert.Equal(HttpStatusCode.Created, aviso.StatusCode);

        using var notificacoes = await _clientePassageiro.GetAsync("/api/v1/notificacoes");
        Assert.Equal(HttpStatusCode.OK, notificacoes.StatusCode);

        using var historico = await _clientePassageiro.GetAsync("/api/v1/historico");
        Assert.Equal(HttpStatusCode.OK, historico.StatusCode);
    }

    /// <summary>RF-F008-12: criar uma viagem informa o utilizador, com 502 (erro) ou 504 (sem resposta), e não grava nada.</summary>
    [Theory]
    [InlineData(false, HttpStatusCode.BadGateway)]
    [InlineData(true, HttpStatusCode.GatewayTimeout)]
    public async Task I110_107_CriarViagemSemMapas_InformaEDeixaTudoComoEstava(bool semResposta, HttpStatusCode esperado)
    {
        DesligarMapas(semResposta);

        using var resposta = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/viagens",
            new { origem = "Porto", destino = "Braga", partidaEm = DateTime.UtcNow.AddDays(1), lugares = 3 });

        Assert.Equal(esperado, resposta.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await LerDetalheAsync(resposta)));
        Assert.Equal(0, _app.NaBaseDeDados(db => db.Viagens.Count()));
    }

    /// <summary>P-19: a rota guardada continua a ser servida sem o Serviço de Mapas.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task I110_108_RotaGuardada_ContinuaDisponivelSemMapas(bool semResposta)
    {
        var viagemId = await ViagemComPassageiroAceiteAsync();
        DesligarMapas(semResposta);

        using var resposta = await _clienteCondutor.GetAsync($"/api/v1/rotas/viagens/{viagemId}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private static async Task<string?> LerDetalheAsync(HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;

        return raiz.TryGetProperty("detail", out var detalhe) ? detalhe.GetString()
            : raiz.TryGetProperty("mensagem", out var mensagem) ? mensagem.GetString()
            : raiz.GetRawText();
    }
}
