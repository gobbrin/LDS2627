using CampusCarpool.Api.Controllers;
using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CampusCarpool.Api.Middleware;

/// <summary>
/// Último recurso para exceções não tratadas nos controllers: responde com o corpo <c>{ erro }</c>, como os outros erros.
/// </summary>
/// <remarks>
/// <see cref="RegraDeNegocioException"/> → 409 (aviso no log), com a mensagem da exceção, que é escrita para o
/// utilizador; lançam-na as entidades numa transição inválida e a gravação num conflito de concorrência. Qualquer
/// outra → 500 (erro no log, com a exceção), com um texto genérico: a mensagem e a stack trace nunca vão para o
/// cliente. Os erros previstos não passam por aqui: saem como <c>Resultado</c> e são traduzidos no
/// <c>RespostasHttp</c> (DC-41). Até à I112 (BE-08) a resposta era um <c>ProblemDetails</c>.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Texto do 500: genérico, para não revelar detalhes internos.</summary>
    public const string MensagemErroInterno = "Ocorreu um erro inesperado.";

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

    /// <summary>Regista a exceção no log e escreve a resposta <c>{ erro }</c>; devolve sempre <c>true</c>.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int codigo;
        string mensagem;

        if (exception is RegraDeNegocioException)
        {
            // Conflito previsto: a mensagem foi escrita para o utilizador.
            LogConflitoNegocio(logger, httpContext.TraceIdentifier, null);
            codigo = StatusCodes.Status409Conflict;
            mensagem = exception.Message;
        }
        else
        {
            // Falha inesperada: o detalhe fica só no log.
            LogExcecaoNaoTratada(logger, exception.GetType().Name, httpContext.TraceIdentifier, exception);
            codigo = StatusCodes.Status500InternalServerError;
            mensagem = MensagemErroInterno;
        }

        httpContext.Response.StatusCode = codigo;
        await httpContext.Response.WriteAsJsonAsync(new CorpoErro(mensagem), cancellationToken);

        return true;
    }
}
