using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioMensagens"/>.</summary>
public sealed class RepositorioMensagens(CampusCarpoolDbContext context) : IRepositorioMensagens
{

    /// <inheritdoc />
    /// <remarks>Guarda a mensagem e persiste as alterações.</remarks>
    public async Task AdicionarAsync(Mensagem mensagem, CancellationToken ct)
    {
        await context.Mensagens.AddAsync(mensagem, ct);
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Recupera a conversa nos dois sentidos, por ordem cronológica. O desempate por <c>Id</c> torna a
    /// ordem estável quando duas mensagens têm a mesma data.
    /// </remarks>
    public async Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct)
    {
        return await context.Mensagens
            .AsNoTracking()
            .Where(m =>
                (m.RemetenteId == utilizadorId &&
                m.DestinatarioId == outroUtilizadorId)
                ||
                (m.RemetenteId == outroUtilizadorId &&
                m.DestinatarioId == utilizadorId))
            .OrderBy(m => m.EnviadoEm)
            .ThenBy(m => m.Id)
            .ToListAsync(ct);
    }
}
