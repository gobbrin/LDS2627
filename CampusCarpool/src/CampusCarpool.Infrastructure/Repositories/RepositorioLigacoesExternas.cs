using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioLigacoesExternas"/>.</summary>
public sealed class RepositorioLigacoesExternas(CampusCarpoolDbContext context) : IRepositorioLigacoesExternas
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LigacaoExterna>> ListarAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.LigacoesExternas
            .AsNoTracking()
            .Where(l => l.UtilizadorId == utilizadorId)
            .OrderBy(l => l.Ordem)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<LigacaoExterna?> ObterAsync(Guid ligacaoId, Guid utilizadorId, CancellationToken ct)
    {
        return await context.LigacoesExternas
            .FirstOrDefaultAsync(l => l.Id == ligacaoId && l.UtilizadorId == utilizadorId, ct);
    }

    /// <inheritdoc />
    public void Adicionar(LigacaoExterna ligacao) => context.LigacoesExternas.Add(ligacao);

    /// <inheritdoc />
    public void Remover(LigacaoExterna ligacao) => context.LigacoesExternas.Remove(ligacao);
}
