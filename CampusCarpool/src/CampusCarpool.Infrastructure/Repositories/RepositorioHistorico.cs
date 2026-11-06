using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Historico;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioHistorico"/>.</summary>
/// <remarks>
/// Consultas só de leitura, sem paginação (AP-10). As conversões de enums para texto são feitas em memória, depois
/// do <c>ToListAsync</c>, para não depender da tradução do fornecedor de BD.
/// </remarks>
public sealed class RepositorioHistorico(CampusCarpoolDbContext db) : IRepositorioHistorico
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemHistorico>> ListarViagensComoCondutorAsync(Guid utilizadorId, CancellationToken ct)
    {
        var viagens = await db.Viagens
            .AsNoTracking()
            .Where(v => v.CondutorId == utilizadorId)
            .OrderByDescending(v => v.PartidaEm)
            .Select(v => new { v.Id, v.Origem, v.Destino, v.PartidaEm, v.Estado, v.LugaresOferecidos })
            .ToListAsync(ct);

        return viagens
            .Select(v => new ViagemHistorico(
                v.Id,
                v.Origem,
                v.Destino,
                v.PartidaEm,
                v.Estado.ToString(),
                v.LugaresOferecidos,
                null,
                null))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemHistorico>> ListarViagensComoPassageiroAsync(Guid utilizadorId, CancellationToken ct)
    {
        var participacoes = await db.Participacoes
            .AsNoTracking()
            // RF-F009-03 / D-029: conta quem chegou a ser aceite, mesmo que a participação tenha sido cancelada
            // depois, pelo passageiro ou pela indisponibilização (P-25).
            .Where(p => p.PassageiroId == utilizadorId && p.AceiteEm != null)
            .OrderByDescending(p => p.Viagem.PartidaEm)
            .Select(p => new
            {
                p.Id,
                p.ViagemId,
                p.Viagem.Origem,
                p.Viagem.Destino,
                p.Viagem.PartidaEm,
                EstadoViagem = p.Viagem.Estado,
                p.Estado
            })
            .ToListAsync(ct);

        return participacoes
            .Select(p => new ViagemHistorico(
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.EstadoViagem.ToString(),
                null,
                p.Id,
                p.Estado.ToString()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PedidoHistorico>> ListarPedidosEfetuadosAsync(Guid utilizadorId, CancellationToken ct)
    {
        var pedidos = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ViagemId,
                p.Viagem.Origem,
                p.Viagem.Destino,
                p.Viagem.PartidaEm,
                p.CriadoEm,
                p.Estado,
                PontoRecolha = p.Recolha,
                PontoDestino = p.Destino
            })
            .ToListAsync(ct);

        return pedidos
            .Select(p => new PedidoHistorico(
                p.Id,
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.CriadoEm,
                p.Estado.ToString(),
                p.PontoRecolha,
                p.PontoDestino))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PedidoHistorico>> ListarPedidosRecebidosAsync(Guid utilizadorId, CancellationToken ct)
    {
        var pedidos = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.Viagem.CondutorId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ViagemId,
                p.Viagem.Origem,
                p.Viagem.Destino,
                p.Viagem.PartidaEm,
                p.CriadoEm,
                p.Estado,
                PontoRecolha = p.Recolha,
                PontoDestino = p.Destino
            })
            .ToListAsync(ct);

        return pedidos
            .Select(p => new PedidoHistorico(
                p.Id,
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.CriadoEm,
                p.Estado.ToString(),
                p.PontoRecolha,
                p.PontoDestino))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificacaoHistorico>> ListarNotificacoesAsync(Guid utilizadorId, CancellationToken ct)
    {
        var notificacoes = await db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId)
            .OrderByDescending(n => n.CriadaEm)
            .Select(n => new
            {
                n.Id,
                n.ViagemId,
                n.Tipo,
                n.Titulo,
                n.Conteudo,
                n.CriadaEm,
                n.LidaEm
            })
            .ToListAsync(ct);

        return notificacoes
            .Select(n => new NotificacaoHistorico(
                n.Id,
                n.ViagemId,
                n.Tipo.ToString(),
                n.Titulo,
                n.Conteudo,
                n.CriadaEm,
                n.LidaEm))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AvisoHistorico>> ListarAvisosAsync(Guid utilizadorId, CancellationToken ct)
    {
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
                    p.Estado == EstadoParticipacao.Aceite))
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AvisoHistorico(
                a.Id,
                a.ViagemId,
                a.Conteudo,
                a.CriadoEm))
            .ToListAsync(ct);
    }
}
