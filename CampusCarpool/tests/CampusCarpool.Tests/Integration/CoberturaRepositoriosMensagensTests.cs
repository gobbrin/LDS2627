using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class CoberturaRepositoriosMensagensTests
{
    [Fact]
    public async Task ObterConversa_DevolveApenasMensagensEntreDoisUtilizadores_Ordenadas()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var primeiro = new Utilizador { Nome = "Primeiro", Email = "primeiro@teste.pt" };
        var segundo = new Utilizador { Nome = "Segundo", Email = "segundo@teste.pt" };
        var terceiro = new Utilizador { Nome = "Terceiro", Email = "terceiro@teste.pt" };
        db.Utilizadores.AddRange(primeiro, segundo, terceiro);
        await db.SaveChangesAsync();

        var repositorio = new RepositorioMensagens(db);
        var data = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var recente = new Mensagem { RemetenteId = primeiro.Id, DestinatarioId = segundo.Id, Conteudo = "recente", EnviadoEm = data.AddMinutes(2) };
        var antiga = new Mensagem { RemetenteId = segundo.Id, DestinatarioId = primeiro.Id, Conteudo = "antiga", EnviadoEm = data };
        var irrelevante = new Mensagem { RemetenteId = terceiro.Id, DestinatarioId = primeiro.Id, Conteudo = "outra conversa", EnviadoEm = data.AddMinutes(1) };
        await repositorio.AdicionarAsync(recente, CancellationToken.None);
        await repositorio.AdicionarAsync(antiga, CancellationToken.None);
        await repositorio.AdicionarAsync(irrelevante, CancellationToken.None);

        var conversa = await repositorio.ObterConversaAsync(primeiro.Id, segundo.Id, CancellationToken.None);
        Assert.Equal(new[] { antiga.Id, recente.Id }, conversa.Select(m => m.Id));
        Assert.All(conversa, m => Assert.False(db.Entry(m).State == EntityState.Modified));
        Assert.Empty(await repositorio.ObterConversaAsync(segundo.Id, terceiro.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UnidadeDeTrabalho_ConfirmaAlteracoesPendentes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var utilizador = new Utilizador { Nome = "Persistido", Email = "persistido@teste.pt" };
        db.Utilizadores.Add(utilizador);
        new UnidadeDeTrabalhoEfCore(db).Confirmar();
        db.ChangeTracker.Clear();
        Assert.Equal("Persistido", (await db.Utilizadores.SingleAsync(u => u.Id == utilizador.Id)).Nome);
    }

    [Fact]
    public void ExcecaoDeRegraDeNegocio_PreservaMensagemECausa()
    {
        var causa = new InvalidOperationException("causa");
        var comCausa = new RegraDeNegocioException("erro", causa);
        Assert.Equal("erro", comCausa.Message);
        Assert.Same(causa, comCausa.InnerException);
        Assert.Equal("simples", new RegraDeNegocioException("simples").Message);
    }

    [Fact]
    public void EntidadesDeConfirmacaoELigacao_PreservamPropriedades()
    {
        var utilizadorId = Guid.NewGuid();
        var expira = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var confirmacao = new ConfirmacaoConta { UtilizadorId = utilizadorId, TokenHash = "hash", ExpiraEm = expira, ConsumidoEm = expira.AddMinutes(-1) };
        var ligacao = new LigacaoExterna { UtilizadorId = utilizadorId, Url = "https://example.org", Descricao = "perfil", Ordem = 2 };
        Assert.NotEqual(Guid.Empty, confirmacao.Id);
        Assert.Equal("hash", confirmacao.TokenHash);
        Assert.Equal(expira, confirmacao.ExpiraEm);
        Assert.Equal(expira.AddMinutes(-1), confirmacao.ConsumidoEm);
        Assert.NotEqual(Guid.Empty, ligacao.Id);
        Assert.Equal(utilizadorId, ligacao.UtilizadorId);
        Assert.Equal("https://example.org", ligacao.Url);
        Assert.Equal("perfil", ligacao.Descricao);
        Assert.Equal(2, ligacao.Ordem);
    }
}
