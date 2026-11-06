using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Mensagem individual, tal como é devolvida ao remetente e na conversa.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ViagemId">Viagem de contexto, se existir.</param>
/// <param name="RemetenteId">Autor.</param>
/// <param name="DestinatarioId">Destinatário.</param>
/// <param name="Conteudo">Texto.</param>
/// <param name="EnviadoEm">Data de envio.</param>
public sealed record MensagemIndividual(
    Guid Id,
    Guid? ViagemId,
    Guid RemetenteId,
    Guid DestinatarioId,
    string Conteudo,
    DateTime EnviadoEm);

/// <summary>Entrada da lista de conversas: o outro utilizador e a última mensagem trocada.</summary>
public sealed record ResumoConversa(
    Guid OutroUtilizadorId,
    string OutroUtilizadorNome,
    string UltimaMensagem,
    DateTime UltimaMensagemEm);

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
    /// RF-F005-02), ou se o remetente ou o destinatário não existir ou não tiver a conta confirmada (P-15).
    /// </returns>
    Task<Resultado<MensagemIndividual>> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct);

    /// <summary>Consulta a conversa entre o utilizador autenticado e o outro utilizador, por ordem cronológica (RF-F005-03, RF-F005-09).</summary>
    /// <remarks>
    /// Como a conversa é sempre a do utilizador autenticado, um terceiro nunca lê uma conversa alheia (RF-F005-04).
    /// Devolve o histórico completo, sem paginação (AP-10).
    /// </remarks>
    /// <returns>A conversa; erro 400 numa conversa consigo próprio, ou se o outro utilizador não existir ou não tiver a conta confirmada.</returns>
    Task<Resultado<List<MensagemIndividual>>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);

    /// <summary>Lista as conversas do utilizador autenticado, da mais recente para a mais antiga (RF-F005-08).</summary>
    /// <remarks>Só inclui conversas em que o utilizador é remetente ou destinatário (RF-F005-04).</remarks>
    Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct);
}
