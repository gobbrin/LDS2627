using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioNotificacoes"/>.</summary>
/// <remarks>
/// As listagens ordenam por data e desempatam por <c>Id</c>, para a paginação ser estável. A paginação é por
/// <c>Skip</c>/<c>Take</c>: uma notificação nova entre dois pedidos desloca os itens de página.
/// </remarks>
public sealed class RepositorioNotificacoes(CampusCarpoolDbContext context) : IRepositorioNotificacoes
{
    /// <inheritdoc />
    public void Adicionar(Notificacao notificacao) => context.Notificacoes.Add(notificacao);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notificacao>> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct)
    {
        return await context.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId)
            .OrderByDescending(n => n.CriadaEm)
            .ThenByDescending(n => n.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Notificacao?> ObterAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct)
    {
        return await context.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == notificacaoId && n.UtilizadorId == utilizadorId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notificacao>> ListarNaoLidasAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Notificacoes
            .Where(n => n.UtilizadorId == utilizadorId && n.LidaEm == null)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> ContarAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Notificacoes
            .CountAsync(n => n.UtilizadorId == utilizadorId, ct);
    }

    /// <inheritdoc />
    public async Task<int> ContarNaoLidasAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await context.Notificacoes
            .CountAsync(n => n.UtilizadorId == utilizadorId && n.LidaEm == null, ct);
    }
}
