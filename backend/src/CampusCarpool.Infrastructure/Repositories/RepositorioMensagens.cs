using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioMensagens"/>.</summary>
public sealed class RepositorioMensagens(CampusCarpoolDbContext context) : IRepositorioMensagens
{
    /// <inheritdoc />
    public void Adicionar(Mensagem mensagem) => context.Mensagens.Add(mensagem);

    /// <inheritdoc />
    public async Task<Mensagem?> ObterPorIdAsync(Guid mensagemId, CancellationToken ct)
    {
        return await context.Mensagens.AsNoTracking().SingleOrDefaultAsync(m => m.Id == mensagemId, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A ordem é a data de envio com desempate pelo <c>Id</c>, para ser estável quando duas mensagens têm a mesma data.
    /// O cursor usa a mesma ordem: entram as mensagens enviadas antes da referência ou no mesmo instante com <c>Id</c>
    /// menor. Pede-se à BD da mais recente para a mais antiga (para o <c>Take</c> ficar com as mais recentes) e
    /// inverte-se no fim, para devolver por ordem cronológica.
    /// </remarks>
    public async Task<IReadOnlyList<Mensagem>> ObterConversaAsync(
        Guid utilizadorId,
        Guid outroUtilizadorId,
        Mensagem? antesDe,
        int tamanho,
        CancellationToken ct)
    {
        IQueryable<Mensagem> conversa = context.Mensagens
            .AsNoTracking()
            .Where(m =>
                (m.RemetenteId == utilizadorId &&
                m.DestinatarioId == outroUtilizadorId)
                ||
                (m.RemetenteId == outroUtilizadorId &&
                m.DestinatarioId == utilizadorId));

        if (antesDe is not null)
        {
            DateTime enviadoEm = antesDe.EnviadoEm;
            Guid id = antesDe.Id;

            conversa = conversa.Where(m => m.EnviadoEm < enviadoEm || (m.EnviadoEm == enviadoEm && m.Id.CompareTo(id) < 0));
        }

        List<Mensagem> maisRecentesPrimeiro = await conversa
            .OrderByDescending(m => m.EnviadoEm)
            .ThenByDescending(m => m.Id)
            .Take(tamanho)
            .ToListAsync(ct);

        maisRecentesPrimeiro.Reverse();
        return maisRecentesPrimeiro;
    }

    /// <inheritdoc />
    /// <remarks>
    /// BE-13: a BD devolve só as mensagens que não têm outra mais recente na mesma conversa (uma por conversa, em vez
    /// de todas as mensagens do utilizador). Duas mensagens no mesmo instante passariam as duas; o agrupamento em
    /// memória, sobre poucas linhas, fica com a primeira.
    /// </remarks>
    public async Task<IReadOnlyList<Mensagem>> ListarUltimasMensagensAsync(Guid utilizadorId, CancellationToken ct)
    {
        List<Mensagem> ultimas = await context.Mensagens
            .AsNoTracking()
            .Where(m => m.RemetenteId == utilizadorId || m.DestinatarioId == utilizadorId)
            .Where(m => !context.Mensagens.Any(outra =>
                ((outra.RemetenteId == m.RemetenteId && outra.DestinatarioId == m.DestinatarioId)
                    || (outra.RemetenteId == m.DestinatarioId && outra.DestinatarioId == m.RemetenteId))
                && outra.EnviadoEm > m.EnviadoEm))
            .OrderByDescending(m => m.EnviadoEm)
            .ThenByDescending(m => m.Id)
            .ToListAsync(ct);

        // Como vêm da mais recente para a mais antiga, a primeira de cada interlocutor é a última da conversa.
        return ultimas
            .GroupBy(m => m.RemetenteId == utilizadorId ? m.DestinatarioId : m.RemetenteId)
            .Select(g => g.First())
            .ToList();
    }
}
