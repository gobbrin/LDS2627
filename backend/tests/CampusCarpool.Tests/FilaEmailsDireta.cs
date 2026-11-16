using CampusCarpool.Application.Abstractions.Integrations;

namespace CampusCarpool.Tests;

/// <summary>
/// Fila de emails para os testes: envia logo, pelo <see cref="IServicoEmail"/> indicado, em vez de esperar pelo
/// trabalhador em segundo plano. Uma falha do envio é engolida, como no <c>TrabalhadorEmails</c> real (BE-12).
/// </summary>
/// <remarks>Assim os testes leem o token enviado logo a seguir ao registo, sem esperas.</remarks>
public sealed class FilaEmailsDireta(IServicoEmail email) : IFilaEmails
{
    /// <summary>Envia já; uma falha do serviço de email não chega a quem enfileirou.</summary>
    public async ValueTask EnfileirarConfirmacaoAsync(string destino, string token, CancellationToken ct)
    {
        try
        {
            await email.EnviarConfirmacaoAsync(destino, token, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            // Como no trabalhador real: a falha fica de fora da resposta.
        }
    }

    /// <summary>Nada fica pendente: a sequência termina logo.</summary>
    public async IAsyncEnumerable<EmailConfirmacaoPendente> LerPendentesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }
}
