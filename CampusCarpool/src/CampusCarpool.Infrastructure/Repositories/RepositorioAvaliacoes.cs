using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Infrastructure.Repositories;
public sealed class RepositorioAvaliacoes(CampusCarpoolDbContext db) : IRepositorioAvaliacoes
{
    public Task<Viagem?> ObterViagemAsync(Guid id, CancellationToken ct) => db.Viagens.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);
    public Task<bool> UtilizadorExisteAsync(Guid id, CancellationToken ct) => db.Utilizadores.AnyAsync(u => u.Id == id, ct);
    public Task<bool> ParticipacaoAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) =>
        db.Participacoes.AnyAsync(p => p.ViagemId == viagemId && p.PassageiroId == passageiroId && p.Estado == EstadoParticipacao.Aceite, ct);
    public Task<Avaliacao?> ObterPorParAsync(Guid avaliadorId, Guid avaliadoId, CancellationToken ct) =>
        db.Avaliacoes.FirstOrDefaultAsync(a => a.AvaliadorId == avaliadorId && a.AvaliadoId == avaliadoId, ct);
    public async Task<Avaliacao> GuardarAsync(Avaliacao avaliacao, CancellationToken ct)
    {
        if (db.Entry(avaliacao).State == EntityState.Detached) db.Avaliacoes.Add(avaliacao);
        await db.SaveChangesAsync(ct);
        return avaliacao;
    }
    public async Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct) =>
        await db.Avaliacoes.AsNoTracking().Where(a => a.AvaliadoId == utilizadorId)
            .OrderByDescending(a => a.CriadoEm).ToListAsync(ct);
}
