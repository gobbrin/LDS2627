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
    /// alterações de colunas passam por cima do <c>CHECK</c> do estado e do índice filtrado dos pedidos. Uma viagem antiga
    /// fora das regras novas (texto longo, mesmas coordenadas) não impede a migration, e os títulos antigos são corrigidos.
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

                INSERT INTO Viagens (Id, CondutorId, Origem, Destino, OrigemLatitude, OrigemLongitude, DestinoLatitude,
                    DestinoLongitude, PartidaEm, LugaresOferecidos, LugaresDisponiveis, Estado)
                VALUES (NEWID(), {condutor}, {new string('a', 230)}, N'Porto', 41.1, -8.6, 41.1, -8.6, '2030-07-16T08:30:00', 3, 3, N'Ativa');

                INSERT INTO Notificacoes (Id, UtilizadorId, Tipo, Titulo, Conteudo, CriadaEm)
                VALUES (NEWID(), {condutor}, N'PedidoAceite', N'PedidoAceite', N'Conteúdo', SYSUTCDATETIME());
                """);

            await db.Database.MigrateAsync();

            var viagens = await db.Viagens.AsNoTracking().OrderBy(v => v.PartidaEm).ToListAsync();
            var viagem = viagens[0];
            Assert.Equal(200, viagens[1].Origem.Length);
            Assert.Equal("Pedido aceite", (await db.Notificacoes.AsNoTracking().SingleAsync()).Titulo);
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

    /// <summary>BE-33 (I112): a conversa tem o índice do par e da data de envio, usado pela paginação por cursor.</summary>
    [Fact]
    public void I112_IndiceDaConversa_ParEDataDeEnvio()
    {
        using var db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlServer("Server=(local);Database=SemLigacao;")
            .Options);

        var indice = Assert.Single(
            db.Model.FindEntityType(typeof(Mensagem))!.GetIndexes(),
            i => i.GetDatabaseName() == "IX_Mensagens_RemetenteId_DestinatarioId_EnviadoEm");

        Assert.Equal(
            [nameof(Mensagem.RemetenteId), nameof(Mensagem.DestinatarioId), nameof(Mensagem.EnviadoEm)],
            indice.Properties.Select(propriedade => propriedade.Name));
    }

    /// <summary>
    /// BE-33 (modelo de dados v07), SQL Server com as migrations: as restrições que só existem em SQL Server recusam a
    /// linha que as viola, pelo nome, e os índices novos existem com as colunas certas.
    /// </summary>
    [SqlServerFact]
    public async Task I112_99_MigrationV07_RestricoesEIndicesNoSqlServer()
    {
        await using var db = new CampusCarpoolDbContext(OpcoesBaseDadosNova());

        try
        {
            await db.Database.MigrateAsync();

            var condutor = new Utilizador { Nome = "Condutor", Email = "v07@estg.ipp.pt", EmailConfirmado = true };
            db.Utilizadores.Add(condutor);
            db.Viagens.Add(NovaViagem(condutor.Id));
            await db.SaveChangesAsync();

            // Cada SQL estraga uma só regra da viagem; a mensagem do SQL Server traz o nome da restrição.
            await AssertRecusaAsync(db, "UPDATE Viagens SET OrigemLatitude = 91", "CK_Viagens_Coordenadas");
            await AssertRecusaAsync(db, "UPDATE Viagens SET DestinoLongitude = -181", "CK_Viagens_Coordenadas");
            await AssertRecusaAsync(db, "UPDATE Viagens SET DistanciaKm = 0", "CK_Viagens_Medidas");
            await AssertRecusaAsync(db, "UPDATE Viagens SET DuracaoMinutos = -5", "CK_Viagens_Medidas");
            await AssertRecusaAsync(db, "UPDATE Utilizadores SET Nome = N'   '", "CK_Utilizadores_Nome");
            await AssertRecusaAsync(db, "UPDATE Viagens SET RotaPontos = N'não é JSON'", "CK_Viagens_Rota");

            // Valores dentro das regras continuam a gravar (a distância e a duração podem ficar por calcular).
            Assert.Equal(1, await db.Database.ExecuteSqlRawAsync(
                "UPDATE Viagens SET DistanciaKm = 12.5, DuracaoMinutos = 20, OrigemLatitude = -90, DestinoLongitude = 180"));
            Assert.Equal(1, await db.Database.ExecuteSqlRawAsync("UPDATE Viagens SET DistanciaKm = NULL, DuracaoMinutos = NULL"));

            // Os 5 índices da v07 criados por esta migration (o 6.º, da conversa, é testado em I112_IndiceDaConversa).
            Assert.Equal("ViagemId,Estado", await ColunasDoIndiceAsync(db, "Participacoes", "IX_Participacoes_ViagemId_Estado"));
            Assert.Equal("DestinatarioId,EnviadoEm", await ColunasDoIndiceAsync(db, "Mensagens", "IX_Mensagens_DestinatarioId_EnviadoEm"));
            Assert.Equal("ViagemId,CriadoEm", await ColunasDoIndiceAsync(db, "Avisos", "IX_Avisos_ViagemId_CriadoEm"));
            Assert.Equal("UtilizadorId,CriadaEm DESC", await ColunasDoIndiceAsync(db, "Notificacoes", "IX_Notificacoes_UtilizadorId_CriadaEm"));
            Assert.Equal(
                "Estado,PartidaEm,+OrigemPesquisa,+DestinoPesquisa,+LugaresDisponiveis",
                await ColunasDoIndiceAsync(db, "Viagens", "IX_Viagens_Estado_PartidaEm"));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// BE-33: a migration v07 sobre uma BD com dados antigos do m3. Um pedido sem pontos não a impede (as restrições
    /// novas são <c>WITH NOCHECK</c>) e continua a poder ser cancelado; um aceite sem data fica com a data do pedido.
    /// </summary>
    [SqlServerFact]
    public async Task I112_100_MigrationV07ComDadosAntigos_NaoFalhaECorrigeAAceitacao()
    {
        await using var db = new CampusCarpoolDbContext(OpcoesBaseDadosNova());

        try
        {
            var migrador = db.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrador.MigrateAsync("20261116213457_I112_IndiceConversaECamposSemUso");

            var condutor = Guid.NewGuid();
            var passageiro = Guid.NewGuid();
            var viagem = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Utilizadores (Id, Nome, Email, PasswordHash, EmailConfirmado, CriadoEm)
                VALUES ({condutor}, N'Condutor', N'antigo.c@estg.ipp.pt', N'hash', 1, SYSUTCDATETIME()),
                       ({passageiro}, N'Passageiro', N'antigo.p@estg.ipp.pt', N'hash', 1, SYSUTCDATETIME());

                INSERT INTO Viagens (Id, CondutorId, Origem, Destino, OrigemLatitude, OrigemLongitude, DestinoLatitude,
                    DestinoLongitude, PartidaEm, LugaresOferecidos, LugaresDisponiveis, Estado, OrigemPesquisa,
                    DestinoPesquisa, PartidaMinutoDoDia)
                VALUES ({viagem}, {condutor}, N'Porto', N'Braga', 41.1, -8.6, 41.5, -8.4, '2030-07-15T08:30:00', 3, 2,
                    N'Ativa', N'PORTO', N'BRAGA', 570);

                INSERT INTO Participacoes (Id, ViagemId, PassageiroId, Estado, CriadoEm)
                VALUES (NEWID(), {viagem}, {passageiro}, N'Aceite', '2030-07-01T10:00:00');
                """);

            await db.Database.MigrateAsync();

            var antigo = await db.Participacoes.AsNoTracking().SingleAsync();
            Assert.Null(antigo.Recolha);
            Assert.Null(antigo.Destino);
            Assert.Equal(new DateTime(2030, 7, 1, 10, 0, 0, DateTimeKind.Utc), antigo.AceiteEm);

            // Cancelar só mexe no estado: o SQL Server não volta a verificar o CHECK dos pontos.
            Assert.Equal(1, await db.Database.ExecuteSqlRawAsync("UPDATE Participacoes SET Estado = N'Cancelada'"));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    // Executa o SQL e confirma que o SQL Server o recusa pela restrição indicada.
    private static async Task AssertRecusaAsync(CampusCarpoolDbContext db, string sql, string restricao)
    {
        var erro = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sql));

        Assert.Contains(restricao, erro.Message, StringComparison.Ordinal);
    }

    // Colunas de um índice, pela ordem: «DESC» nas descendentes e «+» nas incluídas.
    private static async Task<string> ColunasDoIndiceAsync(CampusCarpoolDbContext db, string tabela, string indice)
    {
        var colunas = await db.Database.SqlQuery<string>($"""
            SELECT (CASE WHEN ic.is_included_column = 1 THEN '+' ELSE '' END) + c.name
                   + (CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END) AS Value
            FROM sys.indexes AS i
            JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID({tabela}) AND i.name = {indice}
            ORDER BY ic.is_included_column, ic.key_ordinal, ic.index_column_id
            """).ToListAsync();

        return string.Join(",", colunas);
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
