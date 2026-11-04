using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioViagens"/>.</summary>
public sealed class RepositorioViagens(CampusCarpoolDbContext context) : IRepositorioViagens
{
    /// <inheritdoc />
    /// <remarks>
    /// Obtém uma viagem pelo seu identificador.
    /// A entidade fica rastreada para permitir alterações posteriores; a <c>Versao</c> lida aqui é a que o
    /// SQL Server compara ao gravar.
    /// </remarks>
    public async Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Viagens.FirstOrDefaultAsync(v => v.Id == viagemId, ct);
    }
}
