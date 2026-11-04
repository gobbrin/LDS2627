using CampusCarpool.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>
/// Trabalhador em segundo plano que lê a <see cref="IFilaNotificacoesPush"/> e envia cada push pelo
/// <see cref="IServicoPush"/> (Firebase), fora do pedido HTTP (P-24: entrega em 30 segundos).
/// </summary>
/// <remarks>
/// Cada envio usa um <em>scope</em> de DI novo, porque o <c>ServicoPush</c> depende de um repositório Scoped
/// (o <c>DbContext</c>) e este trabalhador é Singleton. Uma falha é registada no log e não se tenta de novo.
/// </remarks>
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

    /// <summary>Processa a fila até a aplicação parar.</summary>
    /// <param name="stoppingToken">Cancelado quando a aplicação termina.</param>
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
