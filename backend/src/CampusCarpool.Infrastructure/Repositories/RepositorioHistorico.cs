using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioHistorico"/>.</summary>
/// <remarks>
/// Consultas só de leitura, sem seguimento e sem paginação (AP-10). Devolvem entidades; as respostas montam-se no
/// <c>HistoricoService</c> (BE-07).
/// </remarks>
public sealed class RepositorioHistorico(CampusCarpoolDbContext db) : IRepositorioHistorico
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Viagem>> ListarViagensComoCondutorAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await db.Viagens
            .AsNoTracking()
            .Where(v => v.CondutorId == utilizadorId)
            .OrderByDescending(v => v.PartidaEm)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarParticipacoesComoPassageiroAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await db.Participacoes
            .AsNoTracking()
            .Include(p => p.Viagem)
            // RF-F009-03 / D-029: conta quem chegou a ser aceite, mesmo que a participação tenha sido cancelada
            // depois, pelo passageiro ou pela indisponibilização (P-25).
            .Where(p => p.PassageiroId == utilizadorId && p.AceiteEm != null)
            .OrderByDescending(p => p.Viagem.PartidaEm)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarPedidosEfetuadosAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await db.Participacoes
            .AsNoTracking()
            .Include(p => p.Viagem)
            .Where(p => p.PassageiroId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarPedidosRecebidosAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await db.Participacoes
            .AsNoTracking()
            .Include(p => p.Viagem)
            .Where(p => p.Viagem.CondutorId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notificacao>> ListarNotificacoesAsync(Guid utilizadorId, CancellationToken ct)
    {
        return await db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId)
            .OrderByDescending(n => n.CriadaEm)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Aviso>> ListarAvisosAsync(Guid utilizadorId, CancellationToken ct)
    {
        // Avisos das viagens do condutor e das viagens em que o passageiro chegou a ser aceite (AceiteEm preenchido),
        // mesmo que tenha cancelado depois: o mesmo critério do separador Viagens (RF-F009-05, D-029, DEF-BE-024).
        return await db.Avisos
            .AsNoTracking()
            .Where(a =>
                db.Viagens.Any(v =>
                    v.Id == a.ViagemId &&
                    v.CondutorId == utilizadorId)
                ||
                db.Participacoes.Any(p =>
                    p.ViagemId == a.ViagemId &&
                    p.PassageiroId == utilizadorId &&
                    p.AceiteEm != null))
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync(ct);
    }
}
