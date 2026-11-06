using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Os filtros de dia e hora da pesquisa seguem o relógio de Portugal, não UTC (RF-F003-01, P-12, TE-07 (4)).</summary>
/// <remarks>Sobre a <see cref="AplicacaoReal"/>. Em julho Portugal está em UTC+1; em janeiro coincide com UTC.</remarks>
public sealed class PesquisaHoraPortugalHttpTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly HttpClient _cliente;

    public PesquisaHoraPortugalHttpTests()
    {
        _condutor = _app.CriarUtilizador("Condutor");
        _cliente = _app.ClienteDe(_app.CriarUtilizador("Passageiro"));
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _app.Dispose();
    }

    private void CriarViagem(DateTime partidaUtc) => _app.NaBaseDeDados(db =>
    {
        db.Viagens.Add(new Viagem
        {
            CondutorId = _condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            OrigemLatitude = 41.15m,
            OrigemLongitude = -8.61m,
            DestinoLatitude = 41.55m,
            DestinoLongitude = -8.42m,
            PartidaEm = partidaUtc,
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3,
            Estado = EstadoViagem.Ativa
        });

        return db.SaveChanges();
    });

    private async Task<int> TotalAsync(string filtros)
    {
        using var resposta = await _cliente.GetAsync($"/api/v1/pesquisa/viagens?{filtros}");
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("total").GetInt32();
    }

    [Fact]
    public async Task I110_118_HoraMinima_NoVerao_UsaORelogioDePortugal()
    {
        CriarViagem(new DateTime(2030, 7, 15, 8, 30, 0, DateTimeKind.Utc)); // 09:30 em Portugal.

        Assert.Equal(1, await TotalAsync("hora=09:30"));
        Assert.Equal(0, await TotalAsync("hora=09:31"));
    }

    [Fact]
    public async Task I110_119_HoraMinima_NoInverno_CoincideComUtc()
    {
        CriarViagem(new DateTime(2030, 1, 15, 8, 30, 0, DateTimeKind.Utc));

        Assert.Equal(1, await TotalAsync("hora=08:30"));
        Assert.Equal(0, await TotalAsync("hora=08:31"));
    }

    [Fact]
    public async Task I110_120_Dia_PertoDaMeiaNoite_UsaODiaDePortugal()
    {
        CriarViagem(new DateTime(2030, 7, 15, 23, 30, 0, DateTimeKind.Utc)); // 00:30 de dia 16 em Portugal.

        Assert.Equal(1, await TotalAsync("data=2030-07-16"));
        Assert.Equal(0, await TotalAsync("data=2030-07-15"));
    }

    [Fact]
    public async Task I110_121_UltimoDiaPossivel_NaoRebenta()
    {
        Assert.Equal(0, await TotalAsync("data=9999-12-31"));
    }

    [Theory]
    [InlineData(2030, 1, 15, 0)]
    [InlineData(2030, 7, 15, 60)]
    public void I110_122_MinutoDoDia_ContaODesvioDaEstacao(int ano, int mes, int dia, int desvio)
    {
        Assert.Equal(desvio + 90, HoraDePortugal.MinutoDoDia(new DateTime(ano, mes, dia, 1, 30, 0, DateTimeKind.Utc)));
    }
}
