using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> LogConflitoNegocio =
    LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1001, nameof(LogConflitoNegocio)),
        "Conflito de regra de negócio (trace {TraceId})");

    private static readonly Action<ILogger, string, string, Exception?> LogExcecaoNaoTratada =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1002, nameof(LogExcecaoNaoTratada)),
            "Unhandled exception of type {ExceptionType} (trace {TraceId})");
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var eConflitoNegocio = exception is RegraDeNegocioException;

        var statusCode = eConflitoNegocio
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        if (eConflitoNegocio)
        {
            LogConflitoNegocio(
                logger,
                context.TraceIdentifier,
                null);
        }
        else
        {
            LogExcecaoNaoTratada(
                logger,
                exception.GetType().Name,
                context.TraceIdentifier,
                exception);
        }

        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = eConflitoNegocio
                ? "Conflito de regra de negócio"
                : "Erro interno do servidor",
            Detail = eConflitoNegocio
                ? "A operação não pode ser concluída devido ao estado atual do recurso."
                : "Ocorreu um erro inesperado."
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
