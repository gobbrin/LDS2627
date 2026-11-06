using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

/// <summary>Implementa a unidade de trabalho utilizando o Entity Framework Core.</summary>
/// <remarks>
/// Partilha o <see cref="CampusCarpoolDbContext"/> do pedido (Scoped) com os repositórios, por isso grava tudo o
/// que eles carregaram e alteraram. Só os repositórios de participações (pedido novo), avaliações e dispositivos
/// gravam por conta própria: os dois primeiros porque têm de reconhecer a recusa da BD, o último por ser uma operação
/// isolada.
/// </remarks>
public sealed class UnidadeDeTrabalhoEfCore(CampusCarpoolDbContext context) : IUnidadeDeTrabalho
{
    /// <summary>Persiste as alterações pendentes e traduz os conflitos de concorrência em conflitos de regra de negócio.</summary>
    /// <remarks>
    /// Só há conflitos de concorrência na <c>Viagem</c> e na <c>Participacao</c> (coluna <c>Versao</c>), e só em SQL Server. A
    /// <see cref="RegraDeNegocioException"/> chega ao cliente como 409 pelo <c>GlobalExceptionHandler</c>.
    /// </remarks>
    public async Task ConfirmarAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw Conflito(ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TentarConfirmarAsync(CancellationToken ct)
    {
        try
        {
            await ConfirmarAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void DescartarAlteracoes() => context.ChangeTracker.Clear();

    private static RegraDeNegocioException Conflito(DbUpdateConcurrencyException causa) =>
        new("O recurso foi alterado por outra operação. Atualiza os dados e tenta novamente.", causa);
}
