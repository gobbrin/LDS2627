using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

// Implementa a unidade de trabalho utilizando o Entity Framework Core.
public sealed class UnidadeDeTrabalhoEfCore(CampusCarpoolDbContext context) : IUnidadeDeTrabalho
{
    // Persiste as alterações pendentes e traduz conflitos
    // de concorrência em conflitos de regra de negócio.
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
