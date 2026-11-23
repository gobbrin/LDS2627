namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>
/// Fila de notificações push à espera de envio, para o pedido HTTP não esperar pelo Firebase.
/// </summary>
/// <remarks>
/// O <c>NotificacaoService</c> enfileira; o <c>TrabalhadorNotificacoesPush</c> (BackgroundService) lê e envia.
/// A implementação (<c>FilaNotificacoesPush</c>) é só em memória: os push pendentes perdem-se quando o
/// processo reinicia (AP-07, G17). Com a fila cheia, a notificação nova não vai por push, mas continua gravada na
/// caixa de notificações; quem enfileira nunca fica à espera (BE-28).
/// </remarks>
public interface IFilaNotificacoesPush
{
    /// <summary>Coloca uma notificação push na fila, sem esperar: com a fila cheia, descarta-a e regista no log.</summary>
    /// <param name="notificacao">Notificação a enviar (são usados todos os dispositivos ativos do destinatário).</param>
    /// <param name="ct">Cancelamento.</param>
    ValueTask EnfileirarAsync(NotificacaoPushPendente notificacao, CancellationToken ct);

    /// <summary>Lê as notificações pendentes à medida que chegam, até <paramref name="ct"/> ser cancelado.</summary>
    IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct);
}
