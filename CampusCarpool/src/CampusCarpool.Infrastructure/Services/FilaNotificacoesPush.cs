using System.Threading.Channels;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Models;

namespace CampusCarpool.Infrastructure.Services;

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

    public ValueTask EnfileirarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct)
    {
        var notificacao = new NotificacaoPushPendente(utilizadorId, titulo, mensagem, tipo);

        return _fila.Writer.WriteAsync(notificacao, ct);
    }

    public IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct)
    {
        return _fila.Reader.ReadAllAsync(ct);
    }
}
