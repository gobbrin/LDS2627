using CampusCarpool.Application.Models;

namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Fila de notificações push à espera de envio, para o pedido HTTP não esperar pelo Firebase.
/// </summary>
/// <remarks>
/// O <c>NotificacaoService</c> enfileira; o <c>TrabalhadorNotificacoesPush</c> (BackgroundService) lê e envia.
/// A implementação (<c>FilaNotificacoesPush</c>) é só em memória: os push pendentes perdem-se quando o
/// processo reinicia e, com a fila cheia, quem enfileira fica à espera (AP-07).
/// </remarks>
public interface IFilaNotificacoesPush
{
    /// <summary>Coloca uma notificação push na fila.</summary>
    /// <param name="utilizadorId">Destinatário (são usados todos os seus dispositivos ativos).</param>
    /// <param name="titulo">Título do push.</param>
    /// <param name="mensagem">Texto do push.</param>
    /// <param name="tipo">Nome do <c>TipoNotificacao</c>, enviado nos dados do push.</param>
    /// <param name="ct">Cancelamento.</param>
    ValueTask EnfileirarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct);

    /// <summary>Lê as notificações pendentes à medida que chegam, até <paramref name="ct"/> ser cancelado.</summary>
    IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct);
}
