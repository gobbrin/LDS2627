using System.Threading.Channels;
using CampusCarpool.Application.Abstractions.Integrations;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.BackgroundJobs;

/// <summary>Fila em memória (<see cref="Channel{T}"/>) de emails de confirmação à espera de envio (BE-12).</summary>
/// <remarks>
/// Registada como Singleton, como a <see cref="FilaNotificacoesPush"/>. Até <see cref="Capacidade"/> elementos. Com a
/// fila cheia, o email novo é descartado e fica um aviso no log (BE-39, como o push no BE-28): o registo e o reenvio
/// nunca ficam à espera, e o utilizador pode pedir o reenvio da ligação (RF-F001-21). Os elementos perdem-se se o
/// processo reiniciar (AP-07). Um só leitor: o <see cref="TrabalhadorEmails"/>.
/// </remarks>
/// <param name="logger">Log do aviso de fila cheia (sem o email, que é um dado pessoal).</param>
/// <param name="capacidade">Número máximo de elementos; os testes usam um valor pequeno.</param>
public sealed class FilaEmails(ILogger<FilaEmails> logger, int capacidade = FilaEmails.Capacidade) : IFilaEmails
{
    /// <summary>Capacidade por omissão.</summary>
    public const int Capacidade = 1000;

    private static readonly Action<ILogger, Exception?> _filaCheia = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2, "FilaEmailsCheia"),
        "Fila de emails cheia: um email de confirmação não foi enviado; o utilizador pode pedir o reenvio.");

    private readonly Channel<EmailConfirmacaoPendente> _fila =
        Channel.CreateBounded<EmailConfirmacaoPendente>(
            new BoundedChannelOptions(capacidade)
            {
                SingleReader = true,
                SingleWriter = false
            });

    /// <inheritdoc />
    public ValueTask EnfileirarConfirmacaoAsync(string destino, string token, CancellationToken ct)
    {
        // TryWrite nunca espera: devolve false se a fila estiver cheia.
        if (!_fila.Writer.TryWrite(new EmailConfirmacaoPendente(destino, token)))
            _filaCheia(logger, null);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<EmailConfirmacaoPendente> LerPendentesAsync(CancellationToken ct)
    {
        return _fila.Reader.ReadAllAsync(ct);
    }
}
