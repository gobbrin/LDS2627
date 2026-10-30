
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class FalhaTransacaoIntegrationTests
{
    [Fact]
    public async Task IT040_FalhaNaGravacao_NaoPersisteAlteracoesParciais()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var condutor = new Utilizador
        {
            Nome = "Condutor IT040",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        var passageiro = new Utilizador
        {
            Nome = "Passageiro IT040",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.AddRange(condutor, passageiro);

        var viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(2),
            LugaresOferecidos = 1,
            LugaresDisponiveis = 1,
            Estado = "Ativa"
        };

        db.Viagens.Add(viagem);

        var participacao = new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiro.Id,
            Estado = EstadoParticipacao.Pendente
        };

        db.Participacoes.Add(participacao);
        await db.SaveChangesAsync();

        // Simula a aceitação do pedido.
        participacao.Estado = EstadoParticipacao.Aceite;
        viagem.LugaresDisponiveis = 0;

        // Provoca uma falha real de integridade referencial:
        // a participação passa a apontar para um passageiro inexistente.
        participacao.PassageiroId = Guid.NewGuid();

        // A gravação tem de falhar.
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());

        // Consulta com um novo contexto para verificar
        // o que ficou realmente persistido.
        await using var verificacao =
            new CampusCarpoolDbContext(options);

        var viagemFinal = await verificacao.Viagens
            .AsNoTracking()
            .SingleAsync(v => v.Id == viagem.Id);

        var participacaoFinal = await verificacao.Participacoes
            .AsNoTracking()
            .SingleAsync(p => p.Id == participacao.Id);

        Assert.Equal(1, viagemFinal.LugaresDisponiveis);
        Assert.Equal(EstadoParticipacao.Pendente, participacaoFinal.Estado);
        Assert.Equal(passageiro.Id, participacaoFinal.PassageiroId);
    }
}
