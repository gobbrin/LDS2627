
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>IT-023: duas aceitações em simultâneo para o último lugar; só uma pode ficar gravada (coluna <c>Versao</c>).</summary>
/// <remarks>
/// É o único teste com SQL Server real (o <c>rowversion</c> não existe em SQLite). Corre só com a variável <c>CAMPUSCARPOOL_TESTES_SQLSERVER</c> (ver <see cref="SqlServerFactAttribute"/>); sem ela fica "saltado", também na CI.
/// </remarks>
public sealed class ConcorrenciaSqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task IT023_DuasAceitacoes_UltimoLugar_ApenasUmaPersiste()
    {
        var nomeBaseDados = $"CampusCarpool_IT023_{Guid.NewGuid():N}";

        // O servidor vem da variável de ambiente (ver SqlServerFactAttribute);
        // a base de dados tem um nome único por execução.
        var connectionString = new SqlConnectionStringBuilder(
            SqlServerFactAttribute.ObterConnectionString())
        {
            InitialCatalog = nomeBaseDados
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var preparacao = new CampusCarpoolDbContext(options);

        // Esta base de dados tem um nome único para o teste.
        await preparacao.Database.EnsureCreatedAsync();

        try
        {
            var condutor = new Utilizador
            {
                Nome = "Condutor IT023",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };

            var passageiroA = new Utilizador
            {
                Nome = "Passageiro A",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };

            var passageiroB = new Utilizador
            {
                Nome = "Passageiro B",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };

            preparacao.Utilizadores.AddRange(
                condutor, passageiroA, passageiroB);

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

            preparacao.Viagens.Add(viagem);

            var pedidoA = new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiroA.Id,
                Estado = EstadoParticipacao.Pendente
            };

            var pedidoB = new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiroB.Id,
                Estado = EstadoParticipacao.Pendente
            };

            preparacao.Participacoes.AddRange(pedidoA, pedidoB);
            await preparacao.SaveChangesAsync();

            await using var contextoA =
                new CampusCarpoolDbContext(options);

            await using var contextoB =
                new CampusCarpoolDbContext(options);

            // Ambos carregam o mesmo estado inicial.
            var viagemA = await contextoA.Viagens.SingleAsync(
                v => v.Id == viagem.Id);

            var viagemB = await contextoB.Viagens.SingleAsync(
                v => v.Id == viagem.Id);

            var participacaoA = await contextoA.Participacoes.SingleAsync(
                p => p.Id == pedidoA.Id);

            var participacaoB = await contextoB.Participacoes.SingleAsync(
                p => p.Id == pedidoB.Id);

            Assert.Equal(1, viagemA.LugaresDisponiveis);
            Assert.Equal(1, viagemB.LugaresDisponiveis);

            // Primeira operação: aceita A.
            viagemA.LugaresDisponiveis--;
            participacaoA.Estado = EstadoParticipacao.Aceite;

            await contextoA.SaveChangesAsync();

            // Segunda operação: tenta aceitar B com versão antiga.
            viagemB.LugaresDisponiveis--;
            participacaoB.Estado = EstadoParticipacao.Aceite;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => contextoB.SaveChangesAsync());

            // Verificação final numa ligação independente.
            await using var verificacao =
                new CampusCarpoolDbContext(options);

            var viagemFinal = await verificacao.Viagens
                .AsNoTracking()
                .SingleAsync(v => v.Id == viagem.Id);

            var pedidosFinais = await verificacao.Participacoes
                .AsNoTracking()
                .Where(p => p.ViagemId == viagem.Id)
                .ToListAsync();

            Assert.Equal(0, viagemFinal.LugaresDisponiveis);

            Assert.Single(
                pedidosFinais,
                p => p.Estado == EstadoParticipacao.Aceite);

            Assert.Single(
                pedidosFinais,
                p => p.Estado == EstadoParticipacao.Pendente);
        }
        finally
        {
            // Remove apenas a base de dados criada por este teste.
            await preparacao.Database.EnsureDeletedAsync();
        }
    }
}
