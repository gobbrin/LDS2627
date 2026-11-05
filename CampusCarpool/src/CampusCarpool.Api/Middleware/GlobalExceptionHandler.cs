using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Middleware;

/// <summary>
/// Último recurso para exceções não tratadas nos controllers: responde em formato <c>ProblemDetails</c> (RFC 7807).
/// </summary>
/// <remarks>
/// <see cref="RegraDeNegocioException"/> → 409 (aviso no log); qualquer outra → 500 (erro no log, com a exceção).
/// Os textos são genéricos: a mensagem e a stack trace nunca vão para o cliente. Outras exceções conhecidas
/// (<c>DbUpdateConcurrencyException</c>, <c>ArgumentException</c>, ...) também dão 500 se chegarem aqui; a tabela
/// única exceção → código está por fazer (DC-41, fica para m4).
/// </remarks>
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

    /// <summary>Escreve a resposta de erro; devolve <c>true</c> se a escreveu.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var eConflitoNegocio = exception is RegraDeNegocioException;

        var statusCode = eConflitoNegocio
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        if (eConflitoNegocio)
        {
            LogConflitoNegocio(
                logger,
                httpContext.TraceIdentifier,
                null);
        }
        else
        {
            LogExcecaoNaoTratada(
                logger,
                exception.GetType().Name,
                httpContext.TraceIdentifier,
                exception);
        }

        httpContext.Response.StatusCode = statusCode;

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
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
