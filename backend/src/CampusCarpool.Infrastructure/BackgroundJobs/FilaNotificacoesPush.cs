using System.Threading.Channels;
using CampusCarpool.Application.Abstractions.Integrations;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.BackgroundJobs;

/// <summary>Fila em memória (<see cref="Channel{T}"/>) de notificações push à espera de envio.</summary>
/// <remarks>
/// Registada como Singleton. Até <see cref="Capacidade"/> elementos. Com a fila cheia, a notificação nova é descartada
/// e fica um aviso no log (BE-28): o pedido HTTP que a criou nunca fica à espera e a notificação continua gravada na
/// caixa de notificações. Os elementos perdem-se se o processo reiniciar (AP-07, aceite no MVP pela decisão G17).
/// Um só leitor: o <see cref="TrabalhadorNotificacoesPush"/>.
/// </remarks>
/// <param name="logger">Log do aviso de fila cheia.</param>
/// <param name="capacidade">Número máximo de elementos; os testes usam um valor pequeno.</param>
public sealed class FilaNotificacoesPush(ILogger<FilaNotificacoesPush> logger, int capacidade = FilaNotificacoesPush.Capacidade)
    : IFilaNotificacoesPush
{
    /// <summary>Capacidade por omissão.</summary>
    public const int Capacidade = 1000;

    private static readonly Action<ILogger, Guid, Exception?> _filaCheia = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(1, "FilaPushCheia"),
        "Fila de push cheia: a notificação {NotificacaoId} fica só na caixa de notificações.");

    private readonly Channel<NotificacaoPushPendente> _fila =
        Channel.CreateBounded<NotificacaoPushPendente>(
            new BoundedChannelOptions(capacidade)
            {
                SingleReader = true,
                SingleWriter = false
            });

    /// <inheritdoc />
    public ValueTask EnfileirarAsync(NotificacaoPushPendente notificacao, CancellationToken ct)
    {
        // TryWrite nunca espera: devolve false se a fila estiver cheia.
        if (!_fila.Writer.TryWrite(notificacao))
            _filaCheia(logger, notificacao.NotificacaoId, null);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<NotificacaoPushPendente> LerPendentesAsync(CancellationToken ct)
    {
        return _fila.Reader.ReadAllAsync(ct);
    }
}
