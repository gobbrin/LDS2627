using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Tamanhos e restrições do modelo de dados v06 (DC-36, IN-07).</summary>
/// <remarks>
/// As restrições são exercidas com SQL direto, porque a API já não deixa chegar lá estes valores. Os tamanhos são
/// exercidos pela API real: acima do limite da coluna, a resposta tem de ser 400 e não um 500 da BD.
/// </remarks>
public sealed class RestricoesBaseDeDadosTests : IDisposable
{
    private readonly AplicacaoReal _app = new();
    private readonly Utilizador _condutor;
    private readonly Utilizador _passageiro;
    private readonly Viagem _viagem;

    public RestricoesBaseDeDadosTests()
    {
        _condutor = _app.CriarUtilizador("Condutor");
        _passageiro = _app.CriarUtilizador("Passageiro");
        _viagem = new Viagem
        {
            CondutorId = _condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            DestinoLatitude = 41.55m,
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3
        };

        _app.NaBaseDeDados(db =>
        {
            db.Viagens.Add(_viagem);
            db.Participacoes.Add(new Participacao { ViagemId = _viagem.Id, PassageiroId = _passageiro.Id, Recolha = "Maia" });
            return db.SaveChanges();
        });
    }

    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("UPDATE Viagens SET LugaresOferecidos = 9, LugaresDisponiveis = 9")]
    [InlineData("UPDATE Viagens SET LugaresOferecidos = 0, LugaresDisponiveis = 0")]
    [InlineData("UPDATE Viagens SET LugaresDisponiveis = 4")]
    [InlineData("UPDATE Viagens SET LugaresDisponiveis = -1")]
    [InlineData("UPDATE Viagens SET DestinoLatitude = OrigemLatitude, DestinoLongitude = OrigemLongitude")]
    [InlineData("UPDATE Participacoes SET Estado = 'Outro'")]
    [InlineData("UPDATE Participacoes SET RecolhaLatitude = 1, RecolhaLongitude = 2, DestinoLatitude = 1, DestinoLongitude = 2")]
    public void I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(string sql)
    {
        Assert.ThrowsAny<Exception>(() => _app.NaBaseDeDados(db => db.Database.ExecuteSqlRaw(sql)));
    }

    [Fact]
    public void I110_130_NotificacaoComTipoDesconhecido_ABaseDeDadosRecusa()
    {
        var sql = $"""
            INSERT INTO Notificacoes (Id, UtilizadorId, Tipo, Titulo, Conteudo, CriadaEm)
            VALUES ('{Guid.NewGuid()}', '{_condutor.Id}', 'Outro', 'T', 'C', '2030-01-01')
            """;

        Assert.ThrowsAny<Exception>(() => _app.NaBaseDeDados(db => db.Database.ExecuteSqlRaw(sql)));
    }

    [Fact]
    public async Task I110_131_LocalAcimaDoTamanhoDaColuna_Devolve400()
    {
        using var condutor = _app.ClienteDe(_condutor);

        using var resposta = await condutor.PostAsJsonAsync("/api/v1/viagens", new
        {
            origem = new string('a', 201),
            destino = "Porto",
            partidaEm = DateTime.UtcNow.AddDays(2),
            lugares = 3
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task I110_132_TokenDeDispositivoAcimaDoTamanhoDaColuna_Devolve400()
    {
        using var passageiro = _app.ClienteDe(_passageiro);

        using var resposta = await passageiro.PostAsJsonAsync(
            "/api/v1/dispositivos", new { token = new string('a', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
