using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Regras que só o SQL Server aplica, sobre o esquema das migrations (TE-05).</summary>
/// <remarks>
/// O SQLite não tem <c>rowversion</c> e devolve outros erros de índice, por isso estes caminhos nunca correm nos outros
/// testes. Só correm com a variável <c>CAMPUSCARPOOL_TESTES_SQLSERVER</c> (ver <see cref="SqlServerFactAttribute"/>).
/// Cada teste usa uma BD com nome único, apagada no fim.
/// </remarks>
public sealed class RegrasSqlServerTests
{
    /// <summary>Altera a linha noutra ligação mesmo antes de gravar, como outro pedido em simultâneo.</summary>
    private sealed class AlteracaoConcorrente(string ligacao, string tabela, string coluna, Guid id) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await using var outraLigacao = new SqlConnection(ligacao);
            await outraLigacao.OpenAsync(cancellationToken);
            await using var comando = outraLigacao.CreateCommand();
            comando.CommandText = $"UPDATE {tabela} SET {coluna} = {coluna} WHERE Id = @id";
            comando.Parameters.AddWithValue("@id", id);
            await comando.ExecuteNonQueryAsync(cancellationToken);

            return result;
        }
    }

    private static string CadeiaBaseDadosNova() =>
        new SqlConnectionStringBuilder(SqlServerFactAttribute.ObterConnectionString())
        {
            InitialCatalog = $"CampusCarpool_Regras_{Guid.NewGuid():N}"
        }.ConnectionString;

    private static DbContextOptions<CampusCarpoolDbContext> Opcoes(string ligacao) =>
        new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseSqlServer(ligacao).Options;

    private static Utilizador NovoUtilizador(string nome) =>
        new() { Nome = nome, Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };

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

    private static Participacao NovoPedido(Guid viagemId, Guid passageiroId) =>
        new() { ViagemId = viagemId, PassageiroId = passageiroId, Recolha = "Maia" };

    /// <summary>DC-41: um conflito de <c>Versao</c> no serviço real vira <see cref="RegraDeNegocioException"/> (409) e não grava nada.</summary>
    [SqlServerFact]
    public async Task I110_112_ConflitoDeVersao_NoServicoReal_NaoGravaNada()
    {
        var ligacao = CadeiaBaseDadosNova();
        await using var preparacao = new CampusCarpoolDbContext(Opcoes(ligacao));

        try
        {
            await preparacao.Database.MigrateAsync();

            var condutor = NovoUtilizador("Condutor");
            var viagem = NovaViagem(condutor.Id);
            preparacao.Utilizadores.Add(condutor);
            preparacao.Viagens.Add(viagem);
            await preparacao.SaveChangesAsync();

            await using var db = new CampusCarpoolDbContext(
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlServer(ligacao)
                    .AddInterceptors(new AlteracaoConcorrente(ligacao, "Viagens", "LugaresDisponiveis", viagem.Id))
                    .Options);
            var mapas = new MapasFalsos();
            var servico = ServicosDeTeste.CriarViagens(db, mapas.CriarServico());

            await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
                servico.IndisponibilizarAsync(viagem.Id, condutor.Id, CancellationToken.None));

            await using var verificacao = new CampusCarpoolDbContext(Opcoes(ligacao));
            Assert.Equal(EstadoViagem.Ativa, (await verificacao.Viagens.AsNoTracking().SingleAsync()).Estado);
        }
        finally
        {
            await preparacao.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>DO-03: a rejeição só muda a participação, por isso é a <c>Versao</c> dela que recusa a decisão simultânea.</summary>
    [SqlServerFact]
    public async Task I110_128_DecisaoSimultaneaNoMesmoPedido_ASegundaRecebeConflito()
    {
        var ligacao = CadeiaBaseDadosNova();
        await using var preparacao = new CampusCarpoolDbContext(Opcoes(ligacao));

        try
        {
            await preparacao.Database.MigrateAsync();

            var condutor = NovoUtilizador("Condutor");
            var passageiro = NovoUtilizador("Passageiro");
            var viagem = NovaViagem(condutor.Id);
            var pedido = NovoPedido(viagem.Id, passageiro.Id);
            preparacao.Utilizadores.AddRange(condutor, passageiro);
            preparacao.Viagens.Add(viagem);
            preparacao.Participacoes.Add(pedido);
            await preparacao.SaveChangesAsync();

            await using var db = new CampusCarpoolDbContext(
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlServer(ligacao)
                    .AddInterceptors(new AlteracaoConcorrente(ligacao, "Participacoes", "Estado", pedido.Id))
                    .Options);
            var servico = ServicosDeTeste.CriarParticipacoes(db, new MapasFalsos().CriarServico());

            await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
                servico.RejeitarAsync(pedido.Id, condutor.Id, CancellationToken.None));

            await using var verificacao = new CampusCarpoolDbContext(Opcoes(ligacao));
            Assert.Equal(EstadoParticipacao.Pendente, (await verificacao.Participacoes.AsNoTracking().SingleAsync()).Estado);
        }
        finally
        {
            await preparacao.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// RF-F004-06: dois pedidos em simultâneo passam a verificação do serviço; quem os trava é o índice filtrado. O
    /// repositório reconhece o índice pelo nome, por isso este teste também prova que coincide com o das migrations.
    /// Depois de rejeitado, o passageiro pode voltar a pedir, porque o índice só cobre <c>Pendente</c> e <c>Aceite</c>.
    /// </summary>
    [SqlServerFact]
    public async Task I110_113_PedidoRepetido_ORepositorioReconheceOIndiceFiltrado()
    {
        var ligacao = CadeiaBaseDadosNova();
        await using var preparacao = new CampusCarpoolDbContext(Opcoes(ligacao));

        try
        {
            await preparacao.Database.MigrateAsync();

            var condutor = NovoUtilizador("Condutor");
            var passageiro = NovoUtilizador("Passageiro");
            var viagem = NovaViagem(condutor.Id);
            preparacao.Utilizadores.AddRange(condutor, passageiro);
            preparacao.Viagens.Add(viagem);
            await preparacao.SaveChangesAsync();

            async Task<bool> PedirAsync()
            {
                await using var db = new CampusCarpoolDbContext(Opcoes(ligacao));
                return await new RepositorioParticipacoes(db)
                    .AdicionarPedidoAsync(NovoPedido(viagem.Id, passageiro.Id), CancellationToken.None);
            }

            Assert.True(await PedirAsync());
            Assert.False(await PedirAsync());

            var pedido = await preparacao.Participacoes.SingleAsync();
            pedido.Estado = EstadoParticipacao.Rejeitada;
            await preparacao.SaveChangesAsync();

            Assert.True(await PedirAsync());
        }
        finally
        {
            await preparacao.Database.EnsureDeletedAsync();
        }
    }
}
