using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Mensagens individuais entre utilizadores confirmados (F005).</summary>
/// <remarks>
/// Os erros saem como exceções genéricas: o <c>MensagensController</c> converte
/// <see cref="ArgumentException"/> e <see cref="InvalidOperationException"/> em 400 (DO-09).
/// Não há operação para listar as conversas (DEF-BE-018, fica para m4).
/// </remarks>
public interface IMensagemService
{

    /// <summary>Envia uma mensagem em nome do utilizador autenticado e notifica o destinatário (RF-F005-01, RF-F006-06).</summary>
    /// <param name="remetenteId">Utilizador autenticado.</param>
    /// <param name="destinatarioId">Outro utilizador confirmado.</param>
    /// <param name="conteudo">Texto; guardado sem espaços nas pontas.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <exception cref="ArgumentException">Mensagem para si próprio, ou texto vazio ou com mais de 1000 caracteres (P-14, RF-F005-02).</exception>
    /// <exception cref="InvalidOperationException">Remetente ou destinatário inexistente ou sem conta confirmada (P-15).</exception>
    Task<Mensagem> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct);

    /// <summary>Consulta a conversa entre o utilizador autenticado e o outro utilizador, por ordem cronológica (RF-F005-03, RF-F005-09).</summary>
    /// <remarks>Como a conversa é sempre a do utilizador autenticado, um terceiro nunca lê uma conversa alheia (RF-F005-04).</remarks>
    /// <exception cref="ArgumentException">Conversa consigo próprio.</exception>
    /// <exception cref="InvalidOperationException">O outro utilizador não existe ou não tem a conta confirmada.</exception>
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);
}
