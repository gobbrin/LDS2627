using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

/// <summary>Implementa a unidade de trabalho utilizando o Entity Framework Core.</summary>
/// <remarks>
/// Partilha o <see cref="CampusCarpoolDbContext"/> do pedido (Scoped) com os repositórios, por isso grava tudo o
/// que eles carregaram e alteraram. Os repositórios de notificações, mensagens e avaliações gravam por conta
/// própria, fora desta unidade (AP-02).
/// </remarks>
public sealed class UnidadeDeTrabalhoEfCore(CampusCarpoolDbContext context) : IUnidadeDeTrabalho
{
    /// <summary>
    /// Persiste as alterações pendentes e traduz conflitos
    /// de concorrência em conflitos de regra de negócio.
    /// </summary>
    /// <remarks>
    /// Só há conflitos de concorrência na <c>Viagem</c> (coluna <c>Versao</c>) e só em SQL Server. A
    /// <see cref="RegraDeNegocioException"/> chega ao cliente como 409 pelo <c>GlobalExceptionHandler</c>.
    /// </remarks>
    public void Confirmar()
    {
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RegraDeNegocioException(
                "O recurso foi alterado por outra operação. Atualiza os dados e tenta novamente.",
                ex);
        }
    }
}
