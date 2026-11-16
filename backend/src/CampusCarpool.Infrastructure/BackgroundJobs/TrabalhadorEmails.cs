using CampusCarpool.Application.Abstractions.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.BackgroundJobs;

/// <summary>
/// Trabalhador em segundo plano que lê a <see cref="IFilaEmails"/> e envia cada email de confirmação pelo
/// <see cref="IServicoEmail"/> (Brevo), fora do pedido HTTP (BE-12).
/// </summary>
/// <remarks>
/// Igual ao <see cref="TrabalhadorNotificacoesPush"/>: cada envio usa um <em>scope</em> de DI novo (o
/// <c>ServicoEmail</c> é um cliente HTTP tipado, criado por scope). Uma falha fica no log e não se tenta de novo: a
/// conta fica por confirmar e o utilizador pede o reenvio da ligação.
/// </remarks>
public sealed class TrabalhadorEmails(
    IFilaEmails fila,
    IServiceScopeFactory scopeFactory,
    ILogger<TrabalhadorEmails> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogFalhaEmail = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(3001, nameof(LogFalhaEmail)),
        "Falha no envio do email de confirmação");

    /// <summary>Processa a fila até a aplicação parar.</summary>
    /// <param name="stoppingToken">Cancelado quando a aplicação termina.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (EmailConfirmacaoPendente email in fila.LerPendentesAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IServicoEmail servicoEmail = scope.ServiceProvider.GetRequiredService<IServicoEmail>();

                await servicoEmail.EnviarConfirmacaoAsync(email.Destino, email.Token, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Brevo recusou, excedeu o tempo ou está por configurar. O endereço não vai para o log (dado pessoal).
                LogFalhaEmail(logger, ex);
            }
        }
    }
}
