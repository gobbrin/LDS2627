using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Só os pedidos dão push; mensagens, alterações e avisos ficam na aplicação (RF-F006-12, RF-F006-16, RF-F006-17).</summary>
/// <remarks>A <see cref="FilaNotificacoesPushFalsa"/> mostra tudo o que seguiria para o Serviço de Notificações.</remarks>
public sealed class NotificacoesSoNaAplicacaoHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly Utilizador _passageiro;
    private readonly HttpClient _clienteCondutor;
    private readonly HttpClient _clientePassageiro;

    public NotificacoesSoNaAplicacaoHttpTests()
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

    private int NotificacoesDoTipo(TipoNotificacao tipo) =>
        _app.NaBaseDeDados(db => db.Notificacoes.Count(n => n.Tipo == tipo));

    private IEnumerable<string> TiposEmPush() => _app.FilaPush.Notificacoes.Select(n => n.Tipo);

    private async Task<Guid> ViagemComPassageiroAceiteAsync()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(_clienteCondutor);
        await AplicacaoReal.AceitarAsync(
            _clienteCondutor,
            await AplicacaoReal.PedirParticipacaoAsync(_clientePassageiro, viagemId));

        return viagemId;
    }

    /// <summary>RF-F006-12: uma nova mensagem não chega ao Serviço de Notificações.</summary>
    [Fact]
    public async Task I110_103_NovaMensagem_NaoGeraPush()
    {
        using var resposta = await _clienteCondutor.PostAsJsonAsync(
            "/api/v1/mensagens",
            new { destinatarioId = _passageiro.Id, conteudo = "Olá" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(1, NotificacoesDoTipo(TipoNotificacao.NovaMensagem));
        Assert.Empty(TiposEmPush());
    }

    /// <summary>RF-F006-16: alterar, indisponibilizar e a entrada ou saída de passageiros não geram push.</summary>
    [Fact]
    public async Task I110_104_AlteracoesDaViagem_NaoGeramPush()
    {
        var viagemId = await ViagemComPassageiroAceiteAsync();

        using var segundo = _app.ClienteDe(_app.CriarUtilizador("Segundo"));
        var participacaoSegundo = await AplicacaoReal.PedirParticipacaoAsync(segundo, viagemId);
        await AplicacaoReal.AceitarAsync(_clienteCondutor, participacaoSegundo);
        using var saida = await segundo.PostAsync($"/api/v1/participacoes/{participacaoSegundo}/cancelar", null);
        saida.EnsureSuccessStatusCode();

        using var edicao = await _clienteCondutor.PutAsJsonAsync(
            $"/api/v1/viagens/{viagemId}",
            new { partidaEm = DateTime.UtcNow.AddDays(2), lugares = 4 });
        edicao.EnsureSuccessStatusCode();

        using var indisponibilizacao = await _clienteCondutor.PostAsync($"/api/v1/viagens/{viagemId}/indisponibilizar", null);
        indisponibilizacao.EnsureSuccessStatusCode();

        Assert.True(NotificacoesDoTipo(TipoNotificacao.AlteracaoViagem) >= 4);
        Assert.DoesNotContain(nameof(TipoNotificacao.AlteracaoViagem), TiposEmPush());
        Assert.All(TiposEmPush(), tipo => Assert.Contains(tipo, new[]
        {
            nameof(TipoNotificacao.PedidoRecebido),
            nameof(TipoNotificacao.PedidoAceite)
        }));
    }

    /// <summary>RF-F006-17: um aviso do condutor não chega ao Serviço de Notificações.</summary>
    [Fact]
    public async Task I110_105_AvisoDoCondutor_NaoGeraPush()
    {
        var viagemId = await ViagemComPassageiroAceiteAsync();
        var pushAntes = _app.FilaPush.Notificacoes.Count;

        using var resposta = await _clienteCondutor.PostAsJsonAsync(
            $"/api/v1/viagens/{viagemId}/avisos",
            new { conteudo = "Atraso de 5 minutos." });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(1, NotificacoesDoTipo(TipoNotificacao.Aviso));
        Assert.Equal(pushAntes, _app.FilaPush.Notificacoes.Count);
    }
}
