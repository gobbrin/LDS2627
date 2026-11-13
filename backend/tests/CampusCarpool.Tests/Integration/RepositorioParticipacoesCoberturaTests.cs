using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class RepositorioParticipacoesCoberturaTests
{
    [Fact]
    public async Task ListarPorViagem_FiltraEstadoEIgnoraOutrasViagens()
    {
        await using var fixture = await Fixture.CriarAsync();
        var (viagem1, viagem2, passageiro1, passageiro2) = await fixture.PrepararAsync();
        var pendente = await fixture.AdicionarAsync(viagem1, passageiro1, EstadoParticipacao.Pendente);
        var aceite = await fixture.AdicionarAsync(viagem1, passageiro2, EstadoParticipacao.Aceite);
        await fixture.AdicionarAsync(viagem2, passageiro1, EstadoParticipacao.Rejeitada);

        var repo = new RepositorioParticipacoes(fixture.Db);
        var todas = await repo.ListarPorViagemAsync(viagem1, null, CancellationToken.None);
        Assert.Equal(2, todas.Count);
        Assert.Contains(todas, p => p.Id == pendente.Id);
        Assert.Contains(todas, p => p.Id == aceite.Id);

        var aceites = await repo.ListarPorViagemAsync(viagem1, EstadoParticipacao.Aceite, CancellationToken.None);
        Assert.Single(aceites);
        Assert.Equal(aceite.Id, aceites[0].Id);
        Assert.Empty(await repo.ListarPorViagemAsync(viagem2, EstadoParticipacao.Aceite, CancellationToken.None));
    }

    [Fact]
    public async Task ListarPorPassageiro_DevolveSoAsSuasParticipacoes()
    {
        await using var fixture = await Fixture.CriarAsync();
        var (viagem1, viagem2, passageiro1, passageiro2) = await fixture.PrepararAsync();
        var primeira = await fixture.AdicionarAsync(viagem1, passageiro1, EstadoParticipacao.Pendente);
        var segunda = await fixture.AdicionarAsync(viagem2, passageiro1, EstadoParticipacao.Aceite);
        await fixture.AdicionarAsync(viagem1, passageiro2, EstadoParticipacao.Cancelada);

        var repo = new RepositorioParticipacoes(fixture.Db);
        var resultado = await repo.ListarPorPassageiroAsync(passageiro1, CancellationToken.None);
        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, p => p.Id == primeira.Id);
        Assert.Contains(resultado, p => p.Id == segunda.Id);
        Assert.Single(await repo.ListarPorPassageiroAsync(passageiro2, CancellationToken.None));
        Assert.Empty(await repo.ListarPorPassageiroAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(EstadoParticipacao.Pendente, true)]
    [InlineData(EstadoParticipacao.Aceite, true)]
    [InlineData(EstadoParticipacao.Rejeitada, false)]
    [InlineData(EstadoParticipacao.Cancelada, false)]
    public async Task ExisteAtiva_ConsideraApenasPendenteOuAceite(EstadoParticipacao estado, bool esperado)
    {
        await using var fixture = await Fixture.CriarAsync();
        var (viagem1, viagem2, passageiro1, passageiro2) = await fixture.PrepararAsync();
        await fixture.AdicionarAsync(viagem1, passageiro1, estado);

        var repo = new RepositorioParticipacoes(fixture.Db);
        Assert.Equal(esperado, await repo.ExisteAtivaAsync(viagem1, passageiro1, CancellationToken.None));
        Assert.False(await repo.ExisteAtivaAsync(viagem2, passageiro1, CancellationToken.None));
        Assert.False(await repo.ExisteAtivaAsync(viagem1, passageiro2, CancellationToken.None));
    }

    [Fact]
    public async Task ObterPorId_DevolveParticipacaoRastreadaEOuNull()
    {
        await using var fixture = await Fixture.CriarAsync();
        var (viagem1, _, passageiro1, _) = await fixture.PrepararAsync();
        var participacao = await fixture.AdicionarAsync(viagem1, passageiro1, EstadoParticipacao.Pendente);
        fixture.Db.ChangeTracker.Clear();

        var repo = new RepositorioParticipacoes(fixture.Db);
        var encontrada = await repo.ObterPorIdAsync(participacao.Id, CancellationToken.None);
        Assert.NotNull(encontrada);
        Assert.Equal(EntityState.Unchanged, fixture.Db.Entry(encontrada!).State);
        encontrada.Estado = EstadoParticipacao.Aceite;
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        Assert.Equal(EstadoParticipacao.Aceite,
            (await repo.ObterPorIdAsync(participacao.Id, CancellationToken.None))!.Estado);
        Assert.Null(await repo.ObterPorIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; private set; } = null!;

        public static async Task<Fixture> CriarAsync()
        {
            var fixture = new Fixture();
            await fixture._ligacao.OpenAsync();
            fixture.Db = new CampusCarpoolDbContext(
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(fixture._ligacao).Options);
            await fixture.Db.Database.EnsureCreatedAsync();
            return fixture;
        }

        public async Task<(Guid viagem1, Guid viagem2, Guid passageiro1, Guid passageiro2)> PrepararAsync()
        {
            var condutor = new Utilizador { Nome = "Condutor", Email = "condutor@teste.pt" };
            var p1 = new Utilizador { Nome = "P1", Email = "p1@teste.pt" };
            var p2 = new Utilizador { Nome = "P2", Email = "p2@teste.pt" };
            Db.Utilizadores.AddRange(condutor, p1, p2);
            var v1 = new Viagem { CondutorId = condutor.Id, Origem = "Porto", Destino = "Braga", OrigemLatitude = 41.15m, OrigemLongitude = -8.61m, DestinoLatitude = 41.55m, DestinoLongitude = -8.42m, PartidaEm = DateTime.UtcNow.AddDays(2), LugaresOferecidos = 3, LugaresDisponiveis = 3 };
            var v2 = new Viagem { CondutorId = condutor.Id, Origem = "Porto", Destino = "Aveiro", OrigemLatitude = 41.15m, OrigemLongitude = -8.61m, DestinoLatitude = 41.55m, DestinoLongitude = -8.42m, PartidaEm = DateTime.UtcNow.AddDays(3), LugaresOferecidos = 3, LugaresDisponiveis = 3 };
            Db.Viagens.AddRange(v1, v2);
            await Db.SaveChangesAsync();
            return (v1.Id, v2.Id, p1.Id, p2.Id);
        }

        public async Task<Participacao> AdicionarAsync(Guid viagemId, Guid passageiroId, EstadoParticipacao estado)
        {
            var participacao = new Participacao { ViagemId = viagemId, PassageiroId = passageiroId, Estado = estado };
            Db.Participacoes.Add(participacao);
            await Db.SaveChangesAsync();
            return participacao;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _ligacao.DisposeAsync();
        }
    }
}
