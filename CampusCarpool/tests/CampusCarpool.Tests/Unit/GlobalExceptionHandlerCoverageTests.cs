using CampusCarpool.Api.Middleware;
using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class GlobalExceptionHandlerCoverageTests
{
    private sealed class ProblemDetailsRecorder(bool escrever) : IProblemDetailsService
    {
        public ProblemDetailsContext? Recebido { get; private set; }
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Recebido = context;
            return ValueTask.FromResult(escrever);
        }
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Recebido = context;
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task TrataExcecaoComEstadoEDetalhesCorretos(bool negocio, bool escreveu)
    {
        var gravador = new ProblemDetailsRecorder(escreveu);
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, gravador);
        var contexto = new DefaultHttpContext { TraceIdentifier = "teste-123" };
        Exception excecao = negocio ? new RegraDeNegocioException("Conflito") : new InvalidOperationException("Erro interno confidencial");

        var resultado = await handler.TryHandleAsync(contexto, excecao, CancellationToken.None);

        Assert.Equal(escreveu, resultado);
        Assert.Equal(negocio ? 409 : 500, contexto.Response.StatusCode);
        Assert.NotNull(gravador.Recebido);
        Assert.Same(contexto, gravador.Recebido!.HttpContext);
        Assert.Same(excecao, gravador.Recebido.Exception);
        Assert.Equal(negocio ? 409 : 500, gravador.Recebido.ProblemDetails.Status);
        Assert.Equal(negocio ? "Conflito de regra de negócio" : "Erro interno do servidor", gravador.Recebido.ProblemDetails.Title);
        Assert.DoesNotContain("confidencial", gravador.Recebido.ProblemDetails.Detail ?? "");
    }
}
