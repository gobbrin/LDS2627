using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemEmCurso>> ListarAtivasJaPartidasAsync(DateTime agora, CancellationToken ct)
    {
        return await context.Viagens
            .AsNoTracking()
            .Where(v => v.Estado == nameof(EstadoViagem.Ativa) && v.PartidaEm <= agora)
            .Select(v => new ViagemEmCurso(v.Id, v.PartidaEm, v.DuracaoMinutos))
            .ToListAsync(ct);
    }
}
