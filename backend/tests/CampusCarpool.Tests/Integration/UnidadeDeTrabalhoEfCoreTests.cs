using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Conflito de concorrência na gravação: a <see cref="UnidadeDeTrabalhoEfCore"/> traduz o
/// <see cref="DbUpdateConcurrencyException"/> numa <see cref="RegraDeNegocioException"/> (409 na API).
/// </summary>
/// <remarks>
/// O conflito provoca-se com dois contextos na mesma base: um apaga o registo que o outro está a alterar. O EF Core vê
/// que a gravação não alterou nenhuma linha e lança o mesmo erro que a coluna <c>Versao</c> dá em SQL Server.
/// </remarks>
public sealed class UnidadeDeTrabalhoEfCoreTests : IDisposable
{
    private const string MensagemConflito = "O recurso foi alterado por outra operação. Atualiza os dados e tenta novamente.";

    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;
    private readonly CampusCarpoolDbContext _outraOperacao;
    private readonly Utilizador _utilizador;

    /// <summary>Cria a base, grava um utilizador e apaga-o pela «outra operação», sem o primeiro contexto saber.</summary>
    public UnidadeDeTrabalhoEfCoreTests()
    {
        _ligacao.Open();
        DbContextOptions<CampusCarpoolDbContext> opcoes =
            new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(_ligacao).Options;

        _db = new CampusCarpoolDbContext(opcoes);
        _db.Database.CriarEsquemaDeTeste();
        _utilizador = new Utilizador { Nome = "Antes", Email = "conflito@estg.ipp.pt", EmailConfirmado = true };
        _db.Utilizadores.Add(_utilizador);
        _db.SaveChanges();

        _outraOperacao = new CampusCarpoolDbContext(opcoes);
        _outraOperacao.Utilizadores.Where(u => u.Id == _utilizador.Id).ExecuteDelete();
    }

    public void Dispose()
    {
        _outraOperacao.Dispose();
        _db.Dispose();
        _ligacao.Dispose();
    }

    /// <summary><c>ConfirmarAsync</c> lança o conflito como regra de negócio, com a causa original.</summary>
    [Fact]
    public async Task I112_43_ConfirmarComRegistoAlteradoPorOutraOperacao_LancaConflito()
    {
        _utilizador.Nome = "Depois";

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => new UnidadeDeTrabalhoEfCore(_db).ConfirmarAsync(default));

        Assert.Equal(MensagemConflito, erro.Message);
        Assert.IsType<DbUpdateConcurrencyException>(erro.InnerException);
    }

    /// <summary>
    /// <c>TentarConfirmarAsync</c> só devolve as recusas da base de dados (<see cref="DbUpdateException"/>); o conflito
    /// de concorrência continua a sair como exceção.
    /// </summary>
    [Fact]
    public async Task I112_44_TentarConfirmarComConflito_LancaEmVezDeDevolver()
    {
        _utilizador.Nome = "Depois";

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => new UnidadeDeTrabalhoEfCore(_db).TentarConfirmarAsync(default));
    }
}
