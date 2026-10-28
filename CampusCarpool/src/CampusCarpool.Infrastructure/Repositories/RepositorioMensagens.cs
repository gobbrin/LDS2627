using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

public sealed class RepositorioMensagens(CampusCarpoolDbContext context) : IRepositorioMensagens {

    //Guarda a mensagem e persiste as alterações.
    public async Task AdicionarAsync(Mensagem mensagem, CancellationToken ct) {
        await context.Mensagens.AddAsync(mensagem, ct);
        await context.SaveChangesAsync(ct);
    }

    //Recupera a conversa nos dois sentidos, por ordem cronológica.
    public async Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct) {
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