using CampusCarpool.Application.Abstractions.Integrations;

namespace CampusCarpool.Tests;

/// <summary>Fila de push falsa, em memória, para os testes: guarda o que é enfileirado para o teste poder verificar se houve (ou não) push.</summary>
public sealed class FilaNotificacoesPushFalsa : IFilaNotificacoesPush
{
    private readonly List<NotificacaoPushPendente> _notificacoes = [];

    public IReadOnlyList<NotificacaoPushPendente> Notificacoes => _notificacoes;

    public ValueTask EnfileirarAsync(NotificacaoPushPendente notificacao, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _notificacoes.Add(notificacao);

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
