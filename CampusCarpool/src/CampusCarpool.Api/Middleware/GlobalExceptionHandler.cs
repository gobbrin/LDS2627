using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var eConflitoNegocio = exception is RegraDeNegocioException;

        var statusCode = eConflitoNegocio
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        if (eConflitoNegocio)
        {
            logger.LogWarning(
                "Conflito de regra de negócio (trace {TraceId})",
                context.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                "Unhandled exception of type {ExceptionType} (trace {TraceId})",
                exception.GetType().Name,
                context.TraceIdentifier);
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
