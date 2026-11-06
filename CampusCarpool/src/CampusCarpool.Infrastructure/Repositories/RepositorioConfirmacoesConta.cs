using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioConfirmacoesConta"/>.</summary>
public sealed class RepositorioConfirmacoesConta(CampusCarpoolDbContext context) : IRepositorioConfirmacoesConta
{
    /// <inheritdoc />
    public void Adicionar(ConfirmacaoConta confirmacao) => context.ConfirmacoesConta.Add(confirmacao);

    /// <inheritdoc />
    public async Task<ConfirmacaoConta?> ObterValidaAsync(string tokenHash, DateTime agora, CancellationToken ct)
    {
        return await context.ConfirmacoesConta
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash && c.ConsumidoEm == null && c.ExpiraEm > agora, ct);
    }

    /// <inheritdoc />
    public async Task<DateTime?> ObterUltimaExpiracaoAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.ConfirmacoesConta
            .Where(c => c.UtilizadorId == utilizadorId)
            .OrderByDescending(c => c.ExpiraEm)
            .Select(c => (DateTime?)c.ExpiraEm)
            .FirstOrDefaultAsync(ct);
    }
}
