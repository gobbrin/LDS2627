using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioHistorico"/>.</summary>
/// <remarks>
/// Consultas só de leitura e sem seguimento. Cada lista é paginada na base de dados (D-4): conta o total e lê só a
/// página pedida, com o <c>Id</c> a desempatar a ordem para nenhum elemento se repetir ou perder entre páginas.
/// Devolvem entidades; as respostas montam-se no <c>HistoricoService</c> (BE-07).
/// </remarks>
public sealed class RepositorioHistorico(CampusCarpoolDbContext db) : IRepositorioHistorico
{
    /// <inheritdoc />
    public async Task<PaginaDe<Viagem>> ListarViagensComoCondutorAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        IQueryable<Viagem> viagens = db.Viagens
            .AsNoTracking()
            .Where(v => v.CondutorId == utilizadorId);

        int total = await viagens.CountAsync(ct);

        List<Viagem> daPagina = await viagens
            .OrderByDescending(v => v.PartidaEm)
            .ThenBy(v => v.Id)
            .Skip(Paginacao.Saltar(pagina, tamanho))
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaDe<Viagem>(daPagina, total);
    }

    /// <inheritdoc />
    public async Task<PaginaDe<Participacao>> ListarParticipacoesComoPassageiroAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        // RF-F009-03 / D-029: conta quem chegou a ser aceite, mesmo que a participação tenha sido cancelada depois,
        // pelo passageiro ou pela indisponibilização (P-25).
        IQueryable<Participacao> participacoes = db.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == utilizadorId && p.AceiteEm != null);

        int total = await participacoes.CountAsync(ct);

        List<Participacao> daPagina = await participacoes
            .Include(p => p.Viagem)
            .OrderByDescending(p => p.Viagem.PartidaEm)
            .ThenBy(p => p.Id)
            .Skip(Paginacao.Saltar(pagina, tamanho))
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaDe<Participacao>(daPagina, total);
    }

    /// <inheritdoc />
    public async Task<PaginaDe<Participacao>> ListarPedidosEfetuadosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        IQueryable<Participacao> pedidos = db.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == utilizadorId);

        return await PaginaDePedidosAsync(pedidos, pagina, tamanho, ct);
    }

    /// <inheritdoc />
    public async Task<PaginaDe<Participacao>> ListarPedidosRecebidosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        IQueryable<Participacao> pedidos = db.Participacoes
            .AsNoTracking()
            .Where(p => p.Viagem.CondutorId == utilizadorId);

        return await PaginaDePedidosAsync(pedidos, pagina, tamanho, ct);
    }

    /// <inheritdoc />
    public async Task<PaginaDe<Notificacao>> ListarNotificacoesAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        IQueryable<Notificacao> notificacoes = db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId);

        int total = await notificacoes.CountAsync(ct);

        List<Notificacao> daPagina = await notificacoes
            .OrderByDescending(n => n.CriadaEm)
            .ThenBy(n => n.Id)
            .Skip(Paginacao.Saltar(pagina, tamanho))
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaDe<Notificacao>(daPagina, total);
    }

    /// <inheritdoc />
    public async Task<PaginaDe<Aviso>> ListarAvisosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        // Avisos das viagens do condutor e das viagens em que o passageiro chegou a ser aceite (AceiteEm preenchido),
        // mesmo que tenha cancelado depois: o mesmo critério do separador Viagens (RF-F009-05, D-029, DEF-BE-024).
        IQueryable<Aviso> avisos = db.Avisos
            .AsNoTracking()
            .Where(a =>
                db.Viagens.Any(v =>
                    v.Id == a.ViagemId &&
                    v.CondutorId == utilizadorId)
                ||
                db.Participacoes.Any(p =>
                    p.ViagemId == a.ViagemId &&
                    p.PassageiroId == utilizadorId &&
                    p.AceiteEm != null));

        int total = await avisos.CountAsync(ct);

        List<Aviso> daPagina = await avisos
            .OrderByDescending(a => a.CriadoEm)
            .ThenBy(a => a.Id)
            .Skip(Paginacao.Saltar(pagina, tamanho))
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaDe<Aviso>(daPagina, total);
    }

    /// <summary>
    /// Página de pedidos (efetuados ou recebidos), com a viagem carregada, do mais recente para o mais antigo
    /// (RF-F009-04). Os dois métodos de pedidos só diferem no filtro.
    /// </summary>
    private static async Task<PaginaDe<Participacao>> PaginaDePedidosAsync(
        IQueryable<Participacao> pedidos,
        int pagina,
        int tamanho,
        CancellationToken ct)
    {
        int total = await pedidos.CountAsync(ct);

        List<Participacao> daPagina = await pedidos
            .Include(p => p.Viagem)
            .OrderByDescending(p => p.CriadoEm)
            .ThenBy(p => p.Id)
            .Skip(Paginacao.Saltar(pagina, tamanho))
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaDe<Participacao>(daPagina, total);
    }
}
