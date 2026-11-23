namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>
/// Fila de emails de confirmação à espera de envio, para o registo e o reenvio não esperarem pelo Brevo (BE-12).
/// </summary>
/// <remarks>
/// O mesmo padrão dos push (<see cref="IFilaNotificacoesPush"/>): o <c>AutenticacaoService</c> enfileira e o
/// <c>TrabalhadorEmails</c> (BackgroundService) lê e envia pelo <see cref="IServicoEmail"/>. A implementação
/// (<c>FilaEmails</c>) é só em memória: um email pendente perde-se se o processo reiniciar, e o utilizador pede o
/// reenvio da ligação.
/// </remarks>
public interface IFilaEmails
{
    /// <summary>Coloca na fila o email com a ligação de confirmação de conta (RF-F001-06).</summary>
    /// <param name="destino">Email do utilizador.</param>
    /// <param name="token">Token em claro, que segue no URL da ligação.</param>
    /// <param name="ct">Cancelamento.</param>
    ValueTask EnfileirarConfirmacaoAsync(string destino, string token, CancellationToken ct);

    /// <summary>Lê os emails pendentes à medida que chegam, até <paramref name="ct"/> ser cancelado.</summary>
    IAsyncEnumerable<EmailConfirmacaoPendente> LerPendentesAsync(CancellationToken ct);
}

/// <summary>Email de confirmação à espera de envio na <see cref="IFilaEmails"/>.</summary>
/// <param name="Destino">Email do utilizador.</param>
/// <param name="Token">Token em claro da ligação de confirmação.</param>
public sealed record EmailConfirmacaoPendente(string Destino, string Token);
