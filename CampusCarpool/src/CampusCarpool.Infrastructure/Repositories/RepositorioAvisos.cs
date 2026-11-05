using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioAvisos"/>.</summary>
public sealed class RepositorioAvisos(CampusCarpoolDbContext context) : IRepositorioAvisos
{
    /// <inheritdoc />
    public void Adicionar(Aviso aviso) => context.Avisos.Add(aviso);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Aviso>> ListarPorViagemAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Avisos
            .AsNoTracking()
            .Where(a => a.ViagemId == viagemId)
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync(ct);
    }
}
