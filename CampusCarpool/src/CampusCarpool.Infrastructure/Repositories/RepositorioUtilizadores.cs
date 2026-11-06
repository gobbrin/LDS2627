using CampusCarpool.Application.Abstractions.Persistence;
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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> ObterNomesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        return await context.Utilizadores
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ContaConfirmadaAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Utilizadores.AnyAsync(u => u.Id == utilizadorId && u.EmailConfirmado, ct);
    }

    /// <inheritdoc />
    public async Task<Utilizador?> ObterParaAlterarAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Utilizadores.FirstOrDefaultAsync(u => u.Id == utilizadorId, ct);
    }
}
