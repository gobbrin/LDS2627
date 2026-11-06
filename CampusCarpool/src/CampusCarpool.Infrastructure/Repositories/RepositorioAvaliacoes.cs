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
    public async Task<bool> GuardarAsync(Avaliacao avaliacao, CancellationToken ct)
    {
        // Uma entidade "Detached" é nova; uma obtida por ObterPorParAsync já está rastreada e só é gravada.
        var nova = db.Entry(avaliacao).State == EntityState.Detached;
        if (nova) db.Avaliacoes.Add(avaliacao);

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException) when (nova)
        {
            // O índice único do par recusa o segundo de dois pedidos simultâneos. Qualquer outra recusa é uma falha real
            // e segue para o GlobalExceptionHandler.
            db.Entry(avaliacao).State = EntityState.Detached;

            if (await db.Avaliacoes.AsNoTracking().AnyAsync(
                a => a.AvaliadorId == avaliacao.AvaliadorId && a.AvaliadoId == avaliacao.AvaliadoId, ct))
            {
                return false;
            }

            throw;
        }
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct) =>
        await db.Avaliacoes.AsNoTracking().Where(a => a.AvaliadoId == utilizadorId)
            .OrderByDescending(a => a.CriadoEm).ToListAsync(ct);
    /// <inheritdoc />
    /// <remarks>Agrupa-se em memória: o tipo decimal não tem AVG em SQLite (testes).</remarks>
    public async Task<IReadOnlyList<ClassificacaoRecebida>> ListarClassificacoesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Avaliacoes.AsNoTracking().Where(a => ids.Contains(a.AvaliadoId))
            .Select(a => new ClassificacaoRecebida(a.AvaliadoId, a.Classificacao)).ToListAsync(ct);
}
