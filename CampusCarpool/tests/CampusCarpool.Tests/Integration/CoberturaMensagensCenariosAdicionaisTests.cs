using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class CoberturaMensagensCenariosAdicionaisTests
{
    [Fact]
    public async Task ObterConversa_SemMensagens_DevolveListaVazia()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var resultado = await new RepositorioMensagens(db)
            .ObterConversaAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObterConversa_ComMesmaData_OrdenaPorIdentificadorESemTracking()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var a = new Utilizador { Nome = "A", Email = "a@teste.pt" };
        var b = new Utilizador { Nome = "B", Email = "b@teste.pt" };
        db.Utilizadores.AddRange(a, b);
        await db.SaveChangesAsync();

        var instante = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var primeiroId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var segundoId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var repo = new RepositorioMensagens(db);
        repo.Adicionar(new Mensagem { Id = segundoId, RemetenteId = a.Id, DestinatarioId = b.Id, Conteudo = "segunda", EnviadoEm = instante });
        repo.Adicionar(new Mensagem { Id = primeiroId, RemetenteId = b.Id, DestinatarioId = a.Id, Conteudo = "primeira", EnviadoEm = instante });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await repo.ObterConversaAsync(a.Id, b.Id, CancellationToken.None);

        Assert.Equal(new[] { primeiroId, segundoId }, resultado.Select(m => m.Id));
        Assert.All(resultado, m => Assert.Equal(EntityState.Detached, db.Entry(m).State));
    }
}
