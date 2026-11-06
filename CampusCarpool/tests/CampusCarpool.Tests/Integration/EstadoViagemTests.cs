using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Migrations;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — DC-32: o estado da viagem só aceita os nomes de EstadoViagem, sem acento
// (modelo de dados v06), e a migration converte os dados gravados com "Concluída".
// Usa SQLite em memória com EnsureCreated, como os restantes testes de integração.
public sealed class EstadoViagemTests : IDisposable
{
    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;

    public EstadoViagemTests()
    {
        _ligacao.Open();
        _db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseBaseDeTeste(_ligacao).Options);
        _db.Database.CriarEsquemaDeTeste();
    }

    public void Dispose()
    {
        _db.Dispose();
        _ligacao.Dispose();
    }

    private async Task<Viagem> CriarViagemAsync(EstadoViagem estado)
    {
        var condutor = new Utilizador { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
        var viagem = new Viagem
        {
            DestinoLatitude = 41.55m,
            Condutor = condutor,
            Origem = "ESTG",
            Destino = "Porto",
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 2,
            LugaresDisponiveis = 2,
            Estado = estado
        };
        _db.Viagens.Add(viagem);
        await _db.SaveChangesAsync();
        return viagem;
    }

    [Theory]
    [InlineData(EstadoViagem.Ativa)]
    [InlineData(EstadoViagem.Indisponibilizada)]
    [InlineData(EstadoViagem.Concluida)]
    public async Task I110_05_EstadoDeEstadoViagem_AceiteNaBaseDeDados(EstadoViagem estado)
    {
        var viagem = await CriarViagemAsync(estado);

        Assert.Equal(estado, (await _db.Viagens.AsNoTracking().SingleAsync(v => v.Id == viagem.Id)).Estado);
    }

    [Theory]
    [InlineData("Concluída")]
    [InlineData("Cancelada")]
    [InlineData("")]
    public async Task I110_06_EstadoForaDeEstadoViagem_RecusadoPeloCheck(string estado)
    {
        await CriarViagemAsync(EstadoViagem.Ativa);

        // Por SQL direto: pelo modelo já não é possível gravar um estado fora de EstadoViagem.
        var erro = await Assert.ThrowsAnyAsync<System.Data.Common.DbException>(() =>
            _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Viagens SET Estado = {estado}"));

        Assert.Contains("CK_Viagens_Estado", erro.Message);
    }

    [Fact]
    public void I110_07_Migration_ConverteConcluidaComAcentoAntesDoCheck()
    {
        var operacoes = new I110_EstadoViagemConcluidaSemAcento().UpOperations;

        Assert.Collection(
            operacoes,
            primeira =>
            {
                var sql = Assert.IsType<SqlOperation>(primeira).Sql;
                Assert.Contains("SET [Estado] = N'Concluida'", sql);
                Assert.Contains("WHERE [Estado] = N'Concluída'", sql);
            },
            segunda => Assert.Equal("CK_Viagens_Estado", Assert.IsType<AddCheckConstraintOperation>(segunda).Name));
    }
}
