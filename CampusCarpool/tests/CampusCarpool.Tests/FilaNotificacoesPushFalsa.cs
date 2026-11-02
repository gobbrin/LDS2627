using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Models;

namespace CampusCarpool.Tests;

public sealed class FilaNotificacoesPushFalsa : IFilaNotificacoesPush
{
    private readonly List<NotificacaoPushPendente> _notificacoes = [];

    public IReadOnlyList<NotificacaoPushPendente> Notificacoes => _notificacoes;

    public ValueTask EnfileirarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _notificacoes.Add(new NotificacaoPushPendente(utilizadorId, titulo, mensagem, tipo));

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken ct)
    {
        foreach (var notificacao in _notificacoes)
        {
            ct.ThrowIfCancellationRequested();
            yield return notificacao;
        }

        await Task.CompletedTask;
    }
}
