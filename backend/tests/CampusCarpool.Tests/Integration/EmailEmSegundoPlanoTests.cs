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
    /// <summary>
    /// Serviço de email preso: o envio só acaba quando o teste o liberta (<see cref="Libertar"/>), e depois avisa
    /// (<see cref="Enviado"/>). Assim o teste não depende de tempos fixos (R1).
    /// </summary>
    private sealed class EmailPreso : IServicoEmail
    {
        public TaskCompletionSource Libertar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> Enviado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            await Libertar.Task.WaitAsync(ct);
            Enviado.TrySetResult(destino);
        }
    }

    /// <summary>O registo responde sem esperar pelo envio (antes esperava pelo Brevo até 10 s), e o email sai depois.</summary>
    /// <remarks>
    /// O email fica preso até o teste receber a resposta: se o registo esperasse pelo envio, a resposta nunca chegava e
    /// o teste falhava ao fim de <see cref="AplicacaoReal.EsperaMaxima"/>. Antes media-se o tempo (menos de 2 s), o que
    /// falhava com a máquina carregada (R1).
    /// </remarks>
    [Fact]
    public async Task I112_26_Registo_RespondeSemEsperarPeloEmailEOEmailSaiDepois()
    {
        var email = new EmailPreso();

        using var app = new AplicacaoReal(services =>
        {
            // A fila e o serviço reais em vez da fila direta dos outros testes.
            services.RemoveAll<IServicoEmail>();
            services.AddSingleton<IServicoEmail>(email);
            services.RemoveAll<IFilaEmails>();
            services.AddSingleton<IFilaEmails, FilaEmails>();
        });
        using HttpClient cliente = app.CreateClient();

        // A resposta chega com o email ainda preso.
        using HttpResponseMessage resposta = await cliente.PostAsJsonAsync(
                "/api/v1/autenticacao/registo",
                new { nome = "Conta", email = "lento@estg.ipp.pt", password = AplicacaoReal.PalavraPasse })
            .WaitAsync(AplicacaoReal.EsperaMaxima);

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.False(email.Enviado.Task.IsCompleted);

        // Depois de libertado, o trabalhador em segundo plano acaba o envio.
        email.Libertar.SetResult();
        string destino = await email.Enviado.Task.WaitAsync(AplicacaoReal.EsperaMaxima);
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
