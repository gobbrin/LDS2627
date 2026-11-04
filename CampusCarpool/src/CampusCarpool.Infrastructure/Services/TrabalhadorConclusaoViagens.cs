using CampusCarpool.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>
/// Temporizador da conclusão automática das viagens (UC-F002-05, DEF-BE-009).
/// </summary>
/// <remarks>
/// Corre a cada <c>ConclusaoViagens:IntervaloSegundos</c> (60 por omissão; RF-F002-12
/// exige a conclusão no prazo de 5 minutos). A primeira verificação é feita só depois
/// do primeiro intervalo. A conclusão manual pelo condutor mantém-se (PA-01, PA-03).
/// </remarks>
public sealed class TrabalhadorConclusaoViagens(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TrabalhadorConclusaoViagens> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> _falhaVerificacao =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "FalhaVerificacaoConclusaoViagens"),
            "Falha na verificação da conclusão automática das viagens.");

    /// <summary>Intervalo entre verificações.</summary>
    public TimeSpan Intervalo { get; } = TimeSpan.FromSeconds(
        Math.Max(1, configuration.GetValue("ConclusaoViagens:IntervaloSegundos", 60)));

    /// <summary>Executa a verificação a cada <see cref="Intervalo"/> até a aplicação parar.</summary>
    /// <remarks>
    /// Cria um <em>scope</em> de DI por verificação, porque o serviço usa o <c>DbContext</c> (Scoped) e o
    /// trabalhador é Singleton. Com várias instâncias da API, cada uma corre o seu temporizador; a
    /// verificação do estado e a coluna <c>Versao</c> (SQL Server) evitam concluir a mesma viagem duas vezes.
    /// </remarks>
    /// <param name="stoppingToken">Cancelado quando a aplicação termina.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);

        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var servico = scope.ServiceProvider.GetRequiredService<IConclusaoViagensService>();
                    await servico.ConcluirViagensTerminadasAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Uma falha (ex.: base de dados indisponível) não pára o temporizador.
                    _falhaVerificacao(logger, ex);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Paragem normal da aplicação.
        }
    }
}
