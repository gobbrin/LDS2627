using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioUtilizadores"/>.</summary>
public sealed class RepositorioUtilizadores(CampusCarpoolDbContext context) : IRepositorioUtilizadores
{

    /// <inheritdoc />
    /// <remarks>
    /// Procura um utilizador pelo identificador único.
    /// Devolve null caso não exista nenhum utilizador correspondente.
    /// </remarks>
    public async Task<Utilizador?> ObterPorIdAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Utilizadores
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == utilizadorId, ct);
    }
}
