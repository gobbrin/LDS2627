using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Mensagens individuais entre utilizadores confirmados (F005).</summary>
public interface IMensagemService
{
    /// <summary>Envia uma mensagem em nome do utilizador autenticado e notifica o destinatário (RF-F005-01, RF-F006-06).</summary>
    /// <param name="remetenteId">Utilizador autenticado.</param>
    /// <param name="destinatarioId">Outro utilizador confirmado.</param>
    /// <param name="conteudo">Texto; guardado sem espaços nas pontas.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <returns>
    /// A mensagem; erro 400 numa mensagem para si próprio, com texto vazio ou com mais de 1000 caracteres (P-14,
    /// RF-F005-02), ou se o destinatário não existir ou não tiver a conta confirmada (P-15).
    /// </returns>
    Task<Resultado<MensagemIndividual>> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct);

    /// <summary>
    /// Consulta uma página da conversa entre o utilizador autenticado e o outro utilizador, por ordem cronológica
    /// (RF-F005-03, RF-F005-09; paginação por cursor, BE-13).
    /// </summary>
    /// <remarks>
    /// Como a conversa é sempre a do utilizador autenticado, um terceiro nunca lê uma conversa alheia (RF-F005-04).
    /// Sem <paramref name="antesDe"/>, devolve as mensagens mais recentes; com ele, as anteriores a essa mensagem.
    /// Uma página com menos de <paramref name="tamanho"/> mensagens é a última (não há mais antigas).
    /// </remarks>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="outroUtilizadorId">Outro participante.</param>
    /// <param name="antesDe">Id de uma mensagem desta conversa (cursor); <c>null</c> para a página mais recente.</param>
    /// <param name="tamanho">Mensagens por página, de 1 a 100, já validado pelo controller (50 por omissão).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>
    /// A página; erro 400 numa conversa consigo próprio, com <c>antesDe</c> de outra
    /// conversa, ou se o outro utilizador não existir ou não tiver a conta confirmada.
    /// </returns>
    Task<Resultado<List<MensagemIndividual>>> ObterConversaAsync(
        Guid utilizadorId,
        Guid outroUtilizadorId,
        Guid? antesDe,
        int tamanho,
        CancellationToken ct);

    /// <summary>
    /// Lista uma página das conversas do utilizador autenticado, da mais recente para a mais antiga (RF-F005-08, D-4).
    /// </summary>
    /// <remarks>Só inclui conversas em que o utilizador é remetente ou destinatário (RF-F005-04).</remarks>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="pagina">Página, a contar de 1 (validada pelo controller).</param>
    /// <param name="tamanho">Conversas por página, de 1 a 100 (validado pelo controller).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A página, com o total de conversas.</returns>
    Task<PaginaConversas> ListarConversasAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);
}
