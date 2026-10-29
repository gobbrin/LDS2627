using CampusCarpool.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Services;

public sealed class TrabalhadorNotificacoesPush(
    IFilaNotificacoesPush fila,
    IServiceScopeFactory scopeFactory,
    ILogger<TrabalhadorNotificacoesPush> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Guid, Exception?>
        _falhaEnvioNotificacaoPush = LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(2, nameof(FalhaEnvioNotificacaoPush)),
            "Falha ao enviar notificação push para o utilizador {UtilizadorId}.");

    private static void FalhaEnvioNotificacaoPush(ILogger logger, Guid utilizadorId, Exception exception)
    {
        _falhaEnvioNotificacaoPush(logger, utilizadorId, exception);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notificacao in fila.LerPendentesAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var servicoPush = scope.ServiceProvider.GetRequiredService<IServicoPush>();

                await servicoPush.EnviarAsync(
                    notificacao.UtilizadorId,
                    notificacao.Titulo,
                    notificacao.Mensagem,
                    notificacao.Tipo,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                FalhaEnvioNotificacaoPush(logger, notificacao.UtilizadorId, ex);
            }
        }
    }
}
