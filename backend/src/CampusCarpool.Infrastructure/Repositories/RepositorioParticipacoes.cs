using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioParticipacoes"/>.</summary>
public sealed class RepositorioParticipacoes(CampusCarpoolDbContext context) : IRepositorioParticipacoes
{
    // Índice único filtrado (pendente ou aceite) criado no DbContext (I038); usado para reconhecer a violação.
    private const string IndicePedidosAtivos = "IX_Participacoes_ViagemId_PassageiroId";

    /// <summary>
    /// Obtém uma participação pelo identificador.
    /// A entidade permanece rastreada para permitir alterações posteriores.
    /// </summary>
    public async Task<Participacao?> ObterPorIdAsync(Guid participacaoId, CancellationToken ct)
    {
        return await context.Participacoes.FirstOrDefaultAsync(p => p.Id == participacaoId, ct);
    }

    // Lista as participações de uma viagem, podendo filtrar pelo estado.
    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarPorViagemAsync(Guid viagemId, EstadoParticipacao? estado, CancellationToken ct)
    {
        var query = context.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId);

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        return await query.ToListAsync(ct);
    }

    // Lista as participações de um passageiro.
    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarPorPassageiroAsync(Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == passageiroId)
            .ToListAsync(ct);
    }

    // Verifica se o passageiro tem uma participação pendente ou aceite
    // na viagem indicada.
    /// <inheritdoc />
    public async Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes
            .AnyAsync(
                p => p.ViagemId == viagemId
                    && p.PassageiroId == passageiroId
                    && (p.Estado == EstadoParticipacao.Pendente || p.Estado == EstadoParticipacao.Aceite),
                ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistePedidoAsync(Guid viagemId, Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes.AnyAsync(p => p.ViagemId == viagemId && p.PassageiroId == passageiroId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct)
    {
        return await context.Participacoes.AnyAsync(
            p => p.ViagemId == viagemId && p.PassageiroId == passageiroId && p.Estado == EstadoParticipacao.Aceite,
            ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarPendentesParaAlterarAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Participacoes
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Pendente)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Participacao>> ListarAtivasParaAlterarAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Participacoes
            .Where(p => p.ViagemId == viagemId
                && (p.Estado == EstadoParticipacao.Pendente || p.Estado == EstadoParticipacao.Aceite))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassageiroAceite>> ListarPassageirosAceitesAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Aceite)
            .OrderBy(p => p.AceiteEm)
            .Select(p => new PassageiroAceite(p.Id, p.PassageiroId, p.Passageiro.Nome, p.Recolha, p.Destino))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PedidoPendente>> ListarPedidosPendentesAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Pendente)
            .OrderBy(p => p.CriadoEm)
            .Select(p => new PedidoPendente(p.Id, p.PassageiroId, p.Passageiro.Nome, p.Recolha, p.Destino, p.CriadoEm))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> AdicionarPedidoAsync(Participacao pedido, CancellationToken ct)
    {
        context.Participacoes.Add(pedido);

        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        // Erros 2601/2627 do SQL Server. Em SQLite (testes) a exceção é outra e não é apanhada aqui.
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            && sqlEx.Message.Contains(IndicePedidosAtivos, StringComparison.Ordinal))
        {
            return false;
        }
    }
}
