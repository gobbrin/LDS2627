using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// I054: cenários adicionais de persistência relacional previstos na I053.
/// SQLite relacional real em memória; uma base isolada por teste.
/// </summary>
public sealed class I054IntegridadeAdicionalIntegrationTests
{
    private sealed class Ambiente : IDisposable
    {
        private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Ambiente()
        {
            _ligacao.Open();
            Db = new CampusCarpoolDbContext(
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(_ligacao).Options);
            Db.Database.EnsureCreated();
        }

        public async Task<(Utilizador Condutor, Utilizador Passageiro, Viagem Viagem)> PrepararAsync()
        {
            var condutor = new Utilizador { Nome = "Condutor", Email = "condutor@estg.ipp.pt" };
            var passageiro = new Utilizador { Nome = "Passageiro", Email = "passageiro@estg.ipp.pt" };
            Db.Utilizadores.AddRange(condutor, passageiro);
            var viagem = new Viagem { CondutorId = condutor.Id, Origem = "Porto", Destino = "Braga" };
            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();
            return (condutor, passageiro, viagem);
        }

        public void Dispose()
        {
            Db.Dispose();
            _ligacao.Dispose();
        }
    }

    [Fact]
    public async Task IT036_MensagemComDestinatarioInexistente_FalhaChaveEstrangeira()
    {
        using var a = new Ambiente();
        var (condutor, _, viagem) = await a.PrepararAsync();
        a.Db.Mensagens.Add(new Mensagem {
            RemetenteId = condutor.Id, DestinatarioId = Guid.NewGuid(),
            ViagemId = viagem.Id, Conteudo = "Olá"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => a.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task IT036_AvaliacaoComAvaliadoInexistente_FalhaChaveEstrangeira()
    {
        using var a = new Ambiente();
        var (condutor, _, viagem) = await a.PrepararAsync();
        a.Db.Avaliacoes.Add(new Avaliacao {
            ViagemId = viagem.Id, AvaliadorId = condutor.Id,
            AvaliadoId = Guid.NewGuid(), Classificacao = 5
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => a.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task IT036_NotificacaoComUtilizadorInexistente_FalhaChaveEstrangeira()
    {
        using var a = new Ambiente();
        a.Db.Notificacoes.Add(new Notificacao {
            UtilizadorId = Guid.NewGuid(), Titulo = "Pedido", Conteudo = "Novo pedido"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => a.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task IT026_MensagemValida_PersisteEConservaParticipantes()
    {
        using var a = new Ambiente();
        var (condutor, passageiro, viagem) = await a.PrepararAsync();
        var mensagem = new Mensagem {
            RemetenteId = condutor.Id, DestinatarioId = passageiro.Id,
            ViagemId = viagem.Id, Conteudo = "Ponto de encontro"
        };
        a.Db.Mensagens.Add(mensagem);
        await a.Db.SaveChangesAsync();
        a.Db.ChangeTracker.Clear();

        var persistida = await a.Db.Mensagens.SingleAsync(m => m.Id == mensagem.Id);
        Assert.Equal(condutor.Id, persistida.RemetenteId);
        Assert.Equal(passageiro.Id, persistida.DestinatarioId);
        Assert.Equal(viagem.Id, persistida.ViagemId);
        Assert.Equal("Ponto de encontro", persistida.Conteudo);
    }

    [Fact]
    public async Task IT034_AvaliacaoValida_PersisteLigacoes()
    {
        using var a = new Ambiente();
        var (condutor, passageiro, viagem) = await a.PrepararAsync();
        var avaliacao = new Avaliacao {
            ViagemId = viagem.Id, AvaliadorId = passageiro.Id,
            AvaliadoId = condutor.Id, Classificacao = 4
        };
        a.Db.Avaliacoes.Add(avaliacao);
        await a.Db.SaveChangesAsync();
        a.Db.ChangeTracker.Clear();

        var persistida = await a.Db.Avaliacoes.SingleAsync(x => x.Id == avaliacao.Id);
        Assert.Equal(passageiro.Id, persistida.AvaliadorId);
        Assert.Equal(condutor.Id, persistida.AvaliadoId);
        Assert.Equal((byte)4, persistida.Classificacao);
    }
}
