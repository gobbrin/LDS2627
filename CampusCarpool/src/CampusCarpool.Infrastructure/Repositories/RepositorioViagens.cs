using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

public sealed class RepositorioViagens(CampusCarpoolDbContext context) : IRepositorioViagens
{
    // Obtém uma viagem pelo seu identificador.
    // A entidade fica rastreada para permitir alterações posteriores.
    public async Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Viagens.FirstOrDefaultAsync(v => v.Id == viagemId, ct);
    }
}
