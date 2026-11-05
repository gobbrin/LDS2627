using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioAvaliacoes"/>.</summary>
public sealed class RepositorioAvaliacoes(CampusCarpoolDbContext db) : IRepositorioAvaliacoes
{
    /// <inheritdoc />
    public Task<Viagem?> ObterViagemAsync(Guid viagemId, CancellationToken ct) => db.Viagens.AsNoTracking().FirstOrDefaultAsync(v => v.Id == viagemId, ct);
    /// <inheritdoc />
    public Task<bool> UtilizadorExisteAsync(Guid id, CancellationToken ct) => db.Utilizadores.AnyAsync(u => u.Id == id, ct);
    /// <inheritdoc />
    public Task<bool> ParticipacaoAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) =>
        db.Participacoes.AnyAsync(p => p.ViagemId == viagemId && p.PassageiroId == passageiroId && p.Estado == EstadoParticipacao.Aceite, ct);
    /// <inheritdoc />
    public Task<Avaliacao?> ObterPorParAsync(Guid avaliadorId, Guid avaliadoId, CancellationToken ct) =>
        db.Avaliacoes.FirstOrDefaultAsync(a => a.AvaliadorId == avaliadorId && a.AvaliadoId == avaliadoId, ct);
    /// <inheritdoc />
    public async Task<Avaliacao> GuardarAsync(Avaliacao avaliacao, CancellationToken ct)
    {
        // Uma entidade "Detached" é nova; uma obtida por ObterPorParAsync já está rastreada e só é gravada.
        if (db.Entry(avaliacao).State == EntityState.Detached) db.Avaliacoes.Add(avaliacao);
        await db.SaveChangesAsync(ct);
        return avaliacao;
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct) =>
        await db.Avaliacoes.AsNoTracking().Where(a => a.AvaliadoId == utilizadorId)
            .OrderByDescending(a => a.CriadoEm).ToListAsync(ct);
}
