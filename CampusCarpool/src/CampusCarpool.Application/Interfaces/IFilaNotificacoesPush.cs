using CampusCarpool.Application.Models;

namespace CampusCarpool.Application.Interfaces;

public interface IFilaNotificacoesPush
{
    ValueTask EnfileirarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct);

    IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct);
}
