using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Critérios da RTM §5 sobre a criação e a edição de viagens (RF-F002-02, RF-F002-08, RNF-FIA-05), pela API real.</summary>
public sealed class RegrasViagemHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly HttpClient _condutor;

    public RegrasViagemHttpTests() => _condutor = _app.ClienteDe(_app.CriarUtilizador("Condutor"));

    public void Dispose()
    {
        _condutor.Dispose();
        _app.Dispose();
    }

    private Task<HttpResponseMessage> CriarAsync(string partidaEm) => _condutor.PostAsJsonAsync(
        "/api/v1/viagens",
        new { origem = "Porto", destino = "Braga", partidaEm, lugares = 3 });

    private async Task<HttpStatusCode> EditarLugaresAsync(Guid viagemId, int lugares)
    {
        using var resposta = await _condutor.PutAsJsonAsync(
            $"/api/v1/viagens/{viagemId}",
            new { partidaEm = DateTime.UtcNow.AddDays(2), lugares });

        return resposta.StatusCode;
    }

    /// <summary>RF-F002-02: uma data passada devolve erro de validação e não cria viagem.</summary>
    [Fact]
    public async Task I110_99_PartidaNoPassado_Devolve400SemCriarViagem()
    {
        using var resposta = await CriarAsync(DateTime.UtcNow.AddMinutes(-1).ToString("O"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(0, _app.NaBaseDeDados(db => db.Viagens.Count()));
    }

    /// <summary>RF-F002-08: com 2 participações aceites, reduzir para 1 lugar é recusado e para 2 é aceite.</summary>
    [Fact]
    public async Task I110_100_LugaresAbaixoDosAceites_Devolve409()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(_condutor, lugares: 3);
        foreach (var nome in new[] { "Ana", "Bruno" })
        {
            using var passageiro = _app.ClienteDe(_app.CriarUtilizador(nome));
            await AplicacaoReal.AceitarAsync(_condutor, await AplicacaoReal.PedirParticipacaoAsync(passageiro, viagemId));
        }

        Assert.Equal(HttpStatusCode.Conflict, await EditarLugaresAsync(viagemId, 1));
        Assert.Equal(HttpStatusCode.NoContent, await EditarLugaresAsync(viagemId, 2));

        var viagem = _app.NaBaseDeDados(db => db.Viagens.AsNoTracking().Single());
        Assert.Equal(2, viagem.LugaresOferecidos);
        Assert.Equal(0, viagem.LugaresDisponiveis);
    }

    /// <summary>RNF-FIA-05: só se aceitam datas em UTC; sem fuso ou com outro fuso, a API recusa em vez de adivinhar.</summary>
    [Theory]
    [InlineData("-07-01T09:00:00")]
    [InlineData("-07-01T09:00:00+01:00")]
    public async Task I110_101_PartidaQueNaoEstaEmUtc_Devolve400(string resto)
    {
        using var resposta = await CriarAsync($"{DateTime.UtcNow.Year + 1}{resto}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(0, _app.NaBaseDeDados(db => db.Viagens.Count()));
    }

    /// <summary>RNF-FIA-05: a hora em UTC fica gravada sem alteração (09:00 de Lisboa são 08:00 UTC no verão e 09:00 no inverno).</summary>
    [Theory]
    [InlineData("-07-01T08:00:00Z", 7, 8)]
    [InlineData("-12-01T09:00:00Z", 12, 9)]
    public async Task I110_102_PartidaEmUtc_FicaGravadaEmUtc(string resto, int mes, int hora)
    {
        var ano = DateTime.UtcNow.Year + 1;

        using var resposta = await CriarAsync($"{ano}{resto}");

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var partida = _app.NaBaseDeDados(db => db.Viagens.AsNoTracking().Single()).PartidaEm;
        Assert.Equal(new DateTime(ano, mes, 1, hora, 0, 0), partida);
    }
}
