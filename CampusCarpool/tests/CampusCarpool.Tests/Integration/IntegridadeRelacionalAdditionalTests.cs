using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class IntegridadeRelacionalAdditionalTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(connection).Options);
            Db.Database.EnsureCreated();
        }

        public void Dispose() { Db.Dispose(); connection.Dispose(); }
    }

    [Fact]
    public async Task EmailDeUtilizadorTemDeSerUnico()
    {
        using var f = new Fixture();
        f.Db.Utilizadores.Add(new Utilizador { Email = "duplicado@estg.ipp.pt", Nome = "A" });
        await f.Db.SaveChangesAsync();
        f.Db.Utilizadores.Add(new Utilizador { Email = "duplicado@estg.ipp.pt", Nome = "B" });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task TokenDeDispositivoTemDeSerUnico()
    {
        using var f = new Fixture();
        var u = new Utilizador { Email = "a@estg.ipp.pt" };
        f.Db.Utilizadores.Add(u);
        f.Db.Dispositivos.Add(new Dispositivo { UtilizadorId = u.Id, Token = "token-igual" });
        await f.Db.SaveChangesAsync();
        f.Db.Dispositivos.Add(new Dispositivo { UtilizadorId = u.Id, Token = "token-igual" });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task TokenRevogadoJtiTemDeSerUnico()
    {
        using var f = new Fixture();
        f.Db.TokensRevogados.Add(new TokenRevogado { Jti = "mesmo-jti" });
        await f.Db.SaveChangesAsync();
        f.Db.TokensRevogados.Add(new TokenRevogado { Jti = "mesmo-jti" });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task HashDeConfirmacaoTemDeSerUnico()
    {
        using var f = new Fixture();
        var u = new Utilizador { Email = "a@estg.ipp.pt" };
        f.Db.Utilizadores.Add(u);
        f.Db.ConfirmacoesConta.Add(new ConfirmacaoConta { UtilizadorId = u.Id, TokenHash = "hash-repetido" });
        await f.Db.SaveChangesAsync();
        f.Db.ConfirmacoesConta.Add(new ConfirmacaoConta { UtilizadorId = u.Id, TokenHash = "hash-repetido" });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task AvaliacaoDuplicadaParaMesmaViagemEAvaliadorERejeitada()
    {
        using var f = new Fixture();
        var condutor = new Utilizador { Email = "condutor@estg.ipp.pt" };
        var passageiro = new Utilizador { Email = "passageiro@estg.ipp.pt" };
        f.Db.Utilizadores.AddRange(condutor, passageiro);
        var viagem = new Viagem { CondutorId = condutor.Id, Origem = "Porto", Destino = "Braga" };
        f.Db.Viagens.Add(viagem);
        f.Db.Avaliacoes.Add(new Avaliacao { ViagemId = viagem.Id, AvaliadorId = passageiro.Id, AvaliadoId = condutor.Id, Classificacao = 5 });
        await f.Db.SaveChangesAsync();
        f.Db.Avaliacoes.Add(new Avaliacao { ViagemId = viagem.Id, AvaliadorId = passageiro.Id, AvaliadoId = condutor.Id, Classificacao = 4 });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task NaoPermiteEliminarCondutorComViagemAssociada()
    {
        using var f = new Fixture();

        var condutor = new Utilizador
        {
            Email = "condutor@estg.ipp.pt"
        };

        f.Db.Utilizadores.Add(condutor);

        f.Db.Viagens.Add(new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga"
        });

        await f.Db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            f.Db.Utilizadores.Remove(condutor);
            await f.Db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ParticipacaoComPassageiroInexistenteERejeitada()
    {
        using var f = new Fixture();
        var condutor = new Utilizador { Email = "condutor@estg.ipp.pt" };
        f.Db.Utilizadores.Add(condutor);
        var viagem = new Viagem { CondutorId = condutor.Id, Origem = "Porto", Destino = "Braga" };
        f.Db.Viagens.Add(viagem);
        await f.Db.SaveChangesAsync();
        f.Db.Participacoes.Add(new Participacao { ViagemId = viagem.Id, PassageiroId = Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }
}
