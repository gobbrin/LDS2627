using System.Threading.Channels;
using CampusCarpool.Application.Abstractions.Integrations;

namespace CampusCarpool.Infrastructure.BackgroundJobs;

/// <summary>Fila em memória (<see cref="Channel{T}"/>) de emails de confirmação à espera de envio (BE-12).</summary>
/// <remarks>
/// Registada como Singleton, como a <see cref="FilaNotificacoesPush"/>. Até 1000 elementos; com a fila cheia,
/// <see cref="EnfileirarConfirmacaoAsync"/> espera. Os elementos perdem-se se o processo reiniciar (o utilizador pede
/// o reenvio). Um só leitor: o <see cref="TrabalhadorEmails"/>.
/// </remarks>
public sealed class FilaEmails : IFilaEmails
{
    private readonly Channel<EmailConfirmacaoPendente> _fila =
        Channel.CreateBounded<EmailConfirmacaoPendente>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    /// <inheritdoc />
    public ValueTask EnfileirarConfirmacaoAsync(string destino, string token, CancellationToken ct)
    {
        return _fila.Writer.WriteAsync(new EmailConfirmacaoPendente(destino, token), ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<EmailConfirmacaoPendente> LerPendentesAsync(CancellationToken ct)
    {
        return _fila.Reader.ReadAllAsync(ct);
    }
}
