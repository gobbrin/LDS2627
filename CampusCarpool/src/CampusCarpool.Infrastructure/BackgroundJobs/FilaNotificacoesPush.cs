using System.Threading.Channels;
using CampusCarpool.Application.Abstractions.Integrations;

namespace CampusCarpool.Infrastructure.BackgroundJobs;

/// <summary>Fila em memória (<see cref="Channel{T}"/>) de notificações push à espera de envio.</summary>
/// <remarks>
/// Registada como Singleton. Até 1000 elementos; com a fila cheia, <see cref="EnfileirarAsync"/> espera
/// (<c>FullMode.Wait</c>), o que atrasa o pedido HTTP que criou a notificação. Os elementos perdem-se se o
/// processo reiniciar (AP-07, fica para m4). Um só leitor: o <see cref="TrabalhadorNotificacoesPush"/>.
/// </remarks>
public sealed class FilaNotificacoesPush : IFilaNotificacoesPush
{
    private readonly Channel<NotificacaoPushPendente> _fila =
        Channel.CreateBounded<NotificacaoPushPendente>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    /// <inheritdoc />
    public ValueTask EnfileirarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct)
    {
        var notificacao = new NotificacaoPushPendente(utilizadorId, titulo, mensagem, tipo);

        return _fila.Writer.WriteAsync(notificacao, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct)
    {
        return _fila.Reader.ReadAllAsync(ct);
    }
}
