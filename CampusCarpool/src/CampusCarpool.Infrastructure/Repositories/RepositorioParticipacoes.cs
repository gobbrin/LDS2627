using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

public sealed class RepositorioParticipacoes(CampusCarpoolDbContext context) : IRepositorioParticipacoes {

    /// <summary>
    /// Obtém uma participação pelo identificador.
    /// A entidade permanece rastreada para permitir alterações posteriores.
    /// </summary>
    public async Task<Participacao?> ObterPorIdAsync(Guid participacaoId, CancellationToken ct) {
        return await context.Participacoes.FirstOrDefaultAsync(p => p.Id == participacaoId, ct);
    }

    // Lista as participações de uma viagem, podendo filtrar pelo estado.
    public async Task<IReadOnlyList<Participacao>> ListarPorViagemAsync(Guid viagemId, EstadoParticipacao? estado, CancellationToken ct)
    {
        var query = context.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId);

        if(estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        return await query.ToListAsync(ct);
    }

    // Lista as participações de um passageiro.
    public async Task<IReadOnlyList<Participacao>> ListarPorPassageiroAsync(Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == passageiroId)
            .ToListAsync(ct);
    }

    // Verifica se o passageiro tem uma participação pendente ou aceite
    // na viagem indicada.
    public async Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes
            .AnyAsync(
                p => p.ViagemId == viagemId
                    && p.PassageiroId == passageiroId
                    && (p.Estado == EstadoParticipacao.Pendente || p.Estado == EstadoParticipacao.Aceite),
                ct);
    }
}