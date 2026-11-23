using System.Text.Json;
using CampusCarpool.Api.Middleware;
using CampusCarpool.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>O <c>GlobalExceptionHandler</c> escreve <c>{ erro }</c> com o código e o texto esperados (BE-08, I112).</summary>
public sealed class GlobalExceptionHandlerCoverageTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TrataExcecaoComEstadoEMensagemCorretos(bool negocio)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        using var corpo = new MemoryStream();
        var contexto = new DefaultHttpContext { TraceIdentifier = "teste-123" };
        contexto.Response.Body = corpo;
        Exception excecao = negocio ? new RegraDeNegocioException("Conflito") : new InvalidOperationException("Erro interno confidencial");

        var resultado = await handler.TryHandleAsync(contexto, excecao, CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(negocio ? 409 : 500, contexto.Response.StatusCode);

        corpo.Position = 0;
        using JsonDocument json = await JsonDocument.ParseAsync(corpo);
        JsonProperty campo = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("erro", campo.Name);
        Assert.Equal(negocio ? "Conflito" : GlobalExceptionHandler.MensagemErroInterno, campo.Value.GetString());
    }
}
