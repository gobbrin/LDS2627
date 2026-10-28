using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

public interface IMensagemService {
    
    //Envia uma mensagem em nome do utilizador autenticado.
    Task<Mensagem> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct);

    //Consulta a conversa entre o utilizador autenticado e o outro utilizador.
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);
}