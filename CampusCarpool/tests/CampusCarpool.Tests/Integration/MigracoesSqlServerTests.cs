using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>TE-05: aplica todas as migrations a uma BD vazia em SQL Server real e confirma o esquema resultante.</summary>
/// <remarks>
/// Os restantes testes usam SQLite com <c>EnsureCreated</c>, que segue o modelo e ignora as migrations. Só corre com a
/// variável <c>CAMPUSCARPOOL_TESTES_SQLSERVER</c> (ver <see cref="SqlServerFactAttribute"/>). A BD tem um nome único
/// e é apagada no fim.
/// </remarks>
public sealed class MigracoesSqlServerTests
{
    private static DbContextOptions<CampusCarpoolDbContext> OpcoesBaseDadosNova()
    {
        var ligacao = new SqlConnectionStringBuilder(SqlServerFactAttribute.ObterConnectionString())
        {
            InitialCatalog = $"CampusCarpool_Migracoes_{Guid.NewGuid():N}"
        }.ConnectionString;

        return new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseSqlServer(ligacao).Options;
    }

    private static Viagem NovaViagem(Guid condutorId) => new()
    {
        CondutorId = condutorId,
        Origem = "Porto",
        Destino = "Braga",
        DestinoLatitude = 41.55m,
        PartidaEm = DateTime.UtcNow.AddDays(1),
        LugaresOferecidos = 2,
        LugaresDisponiveis = 2
    };

    /// <summary>
    /// O modelo EF coincide com o snapshot das migrations: nenhuma alteração às entidades pede uma migration em falta.
    /// Não liga à base de dados, por isso corre sem SQL Server.
    /// </summary>
    [Fact]
    public void I110_73_ModeloDasEntidades_NaoTemAlteracoesPorMigrar()
    {
        using var db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlServer("Server=(local);Database=SemLigacao;")
            .Options);

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [SqlServerFact]
    public async Task I110_59_Migrations_AplicamSeAUmaBaseDeDadosVazia()
    {
        await using var db = new CampusCarpoolDbContext(OpcoesBaseDadosNova());

        try
        {
            await db.Database.MigrateAsync();

            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            var condutor = new Utilizador { Nome = "Condutor", Email = "migracoes@estg.ipp.pt", EmailConfirmado = true };
            var passageiro = new Utilizador { Nome = "Passageiro", Email = "migracoes.p@estg.ipp.pt", EmailConfirmado = true };
            db.Utilizadores.AddRange(condutor, passageiro);

            // As colunas acrescentadas pela I110 existem e guardam os valores.
            var viagem = NovaViagem(condutor.Id);
            viagem.RotaPontos = "[]";
            db.Viagens.Add(viagem);
            db.Participacoes.Add(new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiro.Id,
                Recolha = "Maia",
                RecolhaLatitude = 41.234567m,
                RecolhaLongitude = -8.621234m,
                AceiteEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var gravada = await db.Viagens.AsNoTracking().SingleAsync();
            Assert.Equal("PORTO", gravada.OrigemPesquisa);
            Assert.Equal("[]", gravada.RotaPontos);
            Assert.Equal(41.234567m, (await db.Participacoes.AsNoTracking().SingleAsync()).RecolhaLatitude);

            // A restrição CK_Viagens_Estado recusa o estado antigo com acento (DC-32).
            await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlRawAsync("UPDATE Viagens SET Estado = N'Concluída'"));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
