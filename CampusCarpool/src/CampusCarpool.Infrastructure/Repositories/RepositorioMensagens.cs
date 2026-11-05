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

    /// <inheritdoc />
    /// <remarks>
    /// As mensagens do utilizador são agrupadas em memória, porque a tradução de "a última de cada grupo" difere entre
    /// SQL Server e SQLite (testes).
    /// </remarks>
    public async Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct)
    {
        var mensagens = await context.Mensagens
            .AsNoTracking()
            .Where(m => m.RemetenteId == utilizadorId || m.DestinatarioId == utilizadorId)
            .OrderByDescending(m => m.EnviadoEm)
            .ThenByDescending(m => m.Id)
            .Select(m => new { m.RemetenteId, m.DestinatarioId, m.Conteudo, m.EnviadoEm })
            .ToListAsync(ct);

        var ultimas = mensagens
            .GroupBy(m => m.RemetenteId == utilizadorId ? m.DestinatarioId : m.RemetenteId)
            .Select(g => (OutroId: g.Key, Ultima: g.First()))
            .ToList();

        var outrosIds = ultimas.Select(u => u.OutroId).ToList();
        var nomes = await context.Utilizadores
            .AsNoTracking()
            .Where(u => outrosIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        return ultimas
            .Select(u => new ResumoConversa(u.OutroId, nomes[u.OutroId], u.Ultima.Conteudo, u.Ultima.EnviadoEm))
            .ToList();
    }
}
