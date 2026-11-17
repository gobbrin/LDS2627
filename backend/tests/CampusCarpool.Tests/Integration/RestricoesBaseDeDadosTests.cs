using System.Globalization;
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
            db.Participacoes.Add(new Participacao { ViagemId = _viagem.Id, PassageiroId = _passageiro.Id, Recolha = "Maia", RecolhaLatitude = 41.23m, RecolhaLongitude = -8.62m });
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

    /// <summary>
    /// BE-33 (modelo de dados v07): cada restrição nova recusa a linha que a viola, pelo nome. O pedido preparado no
    /// construtor tem só a recolha (Maia, com coordenadas) e está pendente; cada SQL estraga uma única regra.
    /// As restrições numéricas da viagem só existem em SQL Server (ver <c>MigracoesSqlServerTests</c>).
    /// </summary>
    [Theory]
    [InlineData("UPDATE Utilizadores SET Nome = '   '", "CK_Utilizadores_Nome")]
    [InlineData("UPDATE Participacoes SET Recolha = NULL, RecolhaLatitude = NULL, RecolhaLongitude = NULL", "CK_Participacoes_UmPonto")]
    [InlineData("UPDATE Participacoes SET RecolhaLatitude = NULL", "CK_Participacoes_CoordRecolha")]
    [InlineData("UPDATE Participacoes SET Destino = 'Trofa'", "CK_Participacoes_CoordDestino")]
    [InlineData("UPDATE Participacoes SET Estado = 'Aceite'", "CK_Participacoes_AceiteEm")]
    public void I112_96_RestricaoDaV07_RecusaALinhaQueAViola(string sql, string restricao)
    {
        var erro = Assert.ThrowsAny<Exception>(() => _app.NaBaseDeDados(db => db.Database.ExecuteSqlRaw(sql)));

        Assert.Contains(restricao, erro.Message, StringComparison.Ordinal);
    }

    /// <summary>BE-33: uma ligação externa só pode ser http ou https, e um aviso não pode ser só espaços.</summary>
    [Theory]
    [InlineData("INSERT INTO LigacoesExternas (Id, UtilizadorId, Url, Ordem) VALUES ('{0}', '{1}', 'ftp://exemplo.pt', 0)", "CK_LigacoesExternas_Url")]
    [InlineData("INSERT INTO Avisos (Id, ViagemId, Conteudo, CriadoEm) VALUES ('{0}', '{2}', '   ', '2030-01-01')", "CK_Avisos_Conteudo")]
    public void I112_97_LigacaoOuAvisoInvalido_ABaseDeDadosRecusa(string modelo, string restricao)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, modelo, Maiusculas(Guid.NewGuid()), Maiusculas(_condutor.Id), Maiusculas(_viagem.Id));

        var erro = Assert.ThrowsAny<Exception>(() => _app.NaBaseDeDados(db => db.Database.ExecuteSqlRaw(sql)));

        Assert.Contains(restricao, erro.Message, StringComparison.Ordinal);
    }

    // O EF guarda os Guid em maiúsculas no SQLite; o SQL Server aceita os dois.
    private static string Maiusculas(Guid id) => id.ToString().ToUpperInvariant();

    /// <summary>BE-33: os dados válidos continuam a gravar (a mesma linha com o ponto e a aceitação completos).</summary>
    [Fact]
    public void I112_98_PedidoAceiteComDataEPontoCompleto_Grava()
    {
        var alteradas = _app.NaBaseDeDados(db => db.Database.ExecuteSqlRaw(
            "UPDATE Participacoes SET Estado = 'Aceite', AceiteEm = '2030-01-01', Destino = 'Trofa', DestinoLatitude = 41.34, DestinoLongitude = -8.56"));

        Assert.Equal(1, alteradas);
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

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "A origem tem no máximo 200 caracteres.");
    }

    [Fact]
    public async Task I110_132_TokenDeDispositivoAcimaDoTamanhoDaColuna_Devolve400()
    {
        using var passageiro = _app.ClienteDe(_passageiro);

        using var resposta = await passageiro.PostAsJsonAsync(
            "/api/v1/dispositivos", new { token = new string('a', 451) });

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "Token FCM inválido.");
    }
}
