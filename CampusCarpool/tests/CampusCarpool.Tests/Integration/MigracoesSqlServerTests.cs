using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// As migrations de 06/11 sobre uma BD com dados: preenchem as colunas de pesquisa das viagens que já existiam e as
    /// alterações de colunas passam por cima do <c>CHECK</c> do estado e do índice filtrado dos pedidos.
    /// </summary>
    [SqlServerFact]
    public async Task I110_133_MigrationsComDados_PreenchemAsColunasDePesquisa()
    {
        await using var db = new CampusCarpoolDbContext(OpcoesBaseDadosNova());

        try
        {
            var migrador = db.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrador.MigrateAsync("20261105212205_I110_PesquisaSemAcentos");

            var condutor = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Utilizadores (Id, Nome, Email, PasswordHash, EmailConfirmado, CriadoEm)
                VALUES ({condutor}, N'Condutor', N'dados@estg.ipp.pt', N'hash', 1, SYSUTCDATETIME());

                INSERT INTO Viagens (Id, CondutorId, Origem, Destino, OrigemLatitude, OrigemLongitude, DestinoLatitude,
                    DestinoLongitude, PartidaEm, LugaresOferecidos, LugaresDisponiveis, Estado)
                VALUES (NEWID(), {condutor}, N'Famalicão', N'Açores', 41.4, -8.5, 37.7, -25.6, '2030-07-15T08:30:00', 3, 3, N'Ativa');
                """);

            await db.Database.MigrateAsync();

            var viagem = await db.Viagens.AsNoTracking().SingleAsync();
            Assert.Equal("FAMALICAO", viagem.OrigemPesquisa);
            Assert.Equal("ACORES", viagem.DestinoPesquisa);
            Assert.Equal(9 * 60 + 30, viagem.PartidaMinutoDoDia);
            Assert.Equal(DateTimeKind.Utc, viagem.PartidaEm.Kind);

            await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlRawAsync("UPDATE Viagens SET Estado = N'Outro'"));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>O índice das avaliações do modelo é o da migration I048 (IN-01): uma avaliação por par, seja qual for a viagem.</summary>
    [Fact]
    public void I110_134_IndiceDasAvaliacoes_EODoPar()
    {
        using var db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlServer("Server=(local);Database=SemLigacao;")
            .Options);

        var unico = Assert.Single(db.Model.FindEntityType(typeof(Avaliacao))!.GetIndexes(), indice => indice.IsUnique);

        Assert.Equal(
            [nameof(Avaliacao.AvaliadorId), nameof(Avaliacao.AvaliadoId)],
            unico.Properties.Select(propriedade => propriedade.Name));
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
