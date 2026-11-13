using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioTokensRevogados"/>.</summary>
public sealed class RepositorioTokensRevogados(CampusCarpoolDbContext context) : IRepositorioTokensRevogados
{
    /// <inheritdoc />
    /// <remarks>Uma consulta à BD por pedido autenticado (índice único em <c>TokensRevogados.Jti</c>).</remarks>
    public async Task<bool> EstaRevogadoAsync(string jti, CancellationToken ct)
    {
        return await context.TokensRevogados.AsNoTracking().AnyAsync(t => t.Jti == jti, ct);
    }

    /// <inheritdoc />
    public void Adicionar(TokenRevogado token) => context.TokensRevogados.Add(token);

    /// <inheritdoc />
    public async Task RemoverExpiradosAsync(DateTime agora, CancellationToken ct)
    {
        await context.TokensRevogados.Where(t => t.ExpiraEm < agora).ExecuteDeleteAsync(ct);
    }
}
