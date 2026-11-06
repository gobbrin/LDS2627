using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Middleware;

/// <summary>
/// Último recurso para exceções não tratadas nos controllers: responde em formato <c>ProblemDetails</c> (RFC 7807).
/// </summary>
/// <remarks>
/// <see cref="RegraDeNegocioException"/> → 409 (aviso no log), com a mensagem da exceção, que é escrita para o
/// utilizador; lançam-na as entidades numa transição inválida e a gravação num conflito de concorrência. Qualquer
/// outra → 500 (erro no log, com a exceção), com um texto genérico: a mensagem e a stack trace nunca vão para o
/// cliente. O texto vem também no campo <c>erro</c>, para o frontend ler todos os erros da mesma maneira. Os erros
/// previstos não passam por aqui: saem como <c>Resultado</c> e são traduzidos no <c>RespostasHttp</c> (DC-41).
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
                ? exception.Message
                : "Ocorreu um erro inesperado."
        };

        problem.Extensions["erro"] = problem.Detail;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
