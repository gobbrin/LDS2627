using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

public sealed class RepositorioUtilizadores(CampusCarpoolDbContext context) : IRepositorioUtilizadores {
    
    //Procura um utilizador pelo identificador único.
    //Devolve null caso não exista nenhum utilizador correspondente.
    public async Task<Utilizador?> ObterPorIdAsync(Guid utilizadorId, CancellationToken ct) {
        return await context.Utilizadores
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == utilizadorId, ct);
    }
}