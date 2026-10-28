using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

public sealed class RepositorioNotificacoes(CampusCarpoolDbContext context) : IRepositorioNotificacoes {
    
    //Adiciona a notificação e guarda as alterações na base de dados.
    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken ct) {
        await context.Notificacoes.AddAsync(notificacao, ct);
        await context.SaveChangesAsync(ct);
    }

    //Consulta as notificações pessoais por ordem cronológica inversa.
    public async Task<IReadOnlyList<Notificacao>> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct) {
        return await context.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId)
            .OrderByDescending(n => n.CriadaEm)
            .ThenByDescending(n => n.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);
    }

    //Obtém apenas uma notificação pertencente ao utilizador indicado.
    public async Task<Notificacao?> ObterAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct) {
        return await context.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == notificacaoId && n.UtilizadorId == utilizadorId, ct);
    }

    //Consulta as notificações ainda não lidas do utilizador.
    public async Task<IReadOnlyList<Notificacao>> ListarNaoLidasAsync(Guid utilizadorId, CancellationToken ct) {
        return await context.Notificacoes
            .Where(n => n.UtilizadorId == utilizadorId && n.LidaEm == null)
            .ToListAsync(ct);
    }

    //Conta todas as notificações do utilizador.
    public async Task<int> ContarAsync(Guid utilizadorId, CancellationToken ct) {
        return await context.Notificacoes
            .CountAsync(n => n.UtilizadorId == utilizadorId, ct);
    }

    //Conta as notificações ainda não lidas do utilizador.
    public async Task<int> ContarNaoLidasAsync(Guid utilizadorId, CancellationToken ct) {
        return await context.Notificacoes
            .CountAsync(n => n.UtilizadorId == utilizadorId && n.LidaEm == null, ct);
    }

    //Guarda as alterações efetuadas às notificações.
    public async Task GuardarAlteracoesAsync(CancellationToken ct) {
        await context.SaveChangesAsync(ct);
    }
    
}