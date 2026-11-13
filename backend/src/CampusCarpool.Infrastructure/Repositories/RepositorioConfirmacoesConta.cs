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
    public async Task<ConfirmacaoConta?> ObterPorHashAsync(string tokenHash, CancellationToken ct)
    {
        return await context.ConfirmacoesConta.FirstOrDefaultAsync(c => c.TokenHash == tokenHash, ct);
    }

    /// <inheritdoc />
    public async Task InvalidarAnterioresAsync(Guid utilizadorId, string tokenHashAtual, DateTime agora, CancellationToken ct)
    {
        var anteriores = await context.ConfirmacoesConta
            .Where(c => c.UtilizadorId == utilizadorId && c.ConsumidoEm == null && c.TokenHash != tokenHashAtual)
            .ToListAsync(ct);

        foreach (var confirmacao in anteriores)
            confirmacao.Invalidar(agora);
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
