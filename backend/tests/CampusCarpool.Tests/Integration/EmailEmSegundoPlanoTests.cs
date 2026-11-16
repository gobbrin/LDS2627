using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Email de confirmação em segundo plano (BE-12, I112): pedido HTTP → <see cref="FilaEmails"/> real →
/// <see cref="TrabalhadorEmails"/> real → serviço de email. E validação do email no reenvio (DEF-BE-045).
/// </summary>
/// <remarks>Os outros testes da <see cref="AplicacaoReal"/> usam a <see cref="FilaEmailsDireta"/>.</remarks>
public sealed class EmailEmSegundoPlanoTests
{
    /// <summary>Serviço de email lento (2 s), que avisa quando acaba o envio.</summary>
    private sealed class EmailLento : IServicoEmail
    {
        public TaskCompletionSource<string> Enviado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            Enviado.TrySetResult(destino);
        }
    }

    /// <summary>O registo responde sem esperar pelo envio (antes esperava pelo Brevo até 10 s), e o email sai depois.</summary>
    [Fact]
    public async Task I112_26_Registo_RespondeSemEsperarPeloEmailEOEmailSaiDepois()
    {
        var email = new EmailLento();

        using var app = new AplicacaoReal(services =>
        {
            // A fila e o serviço reais em vez da fila direta dos outros testes.
            services.RemoveAll<IServicoEmail>();
            services.AddSingleton<IServicoEmail>(email);
            services.RemoveAll<IFilaEmails>();
            services.AddSingleton<IFilaEmails, FilaEmails>();
        });
        using HttpClient cliente = app.CreateClient();
        var relogio = Stopwatch.StartNew();

        using HttpResponseMessage resposta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/registo",
            new { nome = "Conta", email = "lento@estg.ipp.pt", password = AplicacaoReal.PalavraPasse });

        relogio.Stop();
        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.True(relogio.Elapsed < TimeSpan.FromSeconds(2), $"O registo esperou {relogio.Elapsed.TotalSeconds:F1} s.");

        string destino = await email.Enviado.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("lento@estg.ipp.pt", destino);
    }

    /// <summary>DEF-BE-045: um texto que não é email dá 400 com <c>erro</c>; antes dava 202.</summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("a b@estg.ipp.pt")]
    [InlineData("a@@estg.ipp.pt")]
    public async Task I112_27_ReenvioComTextoQueNaoEEmail_Devolve400(string texto)
    {
        using var app = new AplicacaoReal();
        using HttpClient cliente = app.CreateClient();

        using HttpResponseMessage resposta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/reenviar-confirmacao",
            new { email = texto });

        await RespostasDeErro.AssertErroAsync(resposta, HttpStatusCode.BadRequest, "Email inválido.");
    }

    /// <summary>Um email bem formado de outro domínio mantém a resposta neutra (não revela os domínios aceites).</summary>
    [Fact]
    public async Task I112_28_ReenvioDeOutroDominio_ContinuaA202()
    {
        using var app = new AplicacaoReal();
        using HttpClient cliente = app.CreateClient();

        using HttpResponseMessage resposta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/reenviar-confirmacao",
            new { email = "alguem@gmail.com" });

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
    }
}
