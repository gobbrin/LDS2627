using System.Collections.Concurrent;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Trabalhadores em segundo plano da conclusão das viagens (UC-F002-05, RF-F002-12) e dos emails de confirmação
/// (RF-F001-06, BE-12): o ciclo continua depois de uma falha e termina quando a aplicação para.
/// </summary>
/// <remarks>
/// Os trabalhadores são os reais; só o serviço que eles chamam é falso. O teste espera por sinais
/// (<see cref="TaskCompletionSource"/>) com o limite <see cref="AplicacaoReal.EsperaMaxima"/>, nunca por tempos fixos (R1).
/// A lógica da conclusão (que viagens concluir) está em <see cref="ConclusaoViagensTests"/>.
/// </remarks>
public sealed class TrabalhadoresSegundoPlanoTests
{
    /// <summary>Cria um <see cref="IServiceScopeFactory"/> que só conhece o serviço indicado (Singleton).</summary>
    /// <param name="servico">Serviço falso que o trabalhador vai pedir em cada scope.</param>
    private static IServiceScopeFactory ScopesCom<TServico>(TServico servico)
        where TServico : class
    {
        ServiceProvider servicos = new ServiceCollection().AddSingleton(servico).BuildServiceProvider();
        return servicos.GetRequiredService<IServiceScopeFactory>();
    }

    // ---------------------------------------------------------------- conclusão das viagens

    /// <summary>
    /// Serviço de conclusão falso: a primeira verificação falha (como uma base de dados em baixo); a segunda avisa
    /// o teste pelo <see cref="SegundaVerificacao"/> com o «agora» recebido.
    /// </summary>
    private sealed class ConclusaoQueFalhaAPrimeira : IConclusaoViagensService
    {
        private int _chamadas;

        public TaskCompletionSource<DateTime> SegundaVerificacao { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<int> ConcluirViagensTerminadasAsync(DateTime agora, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _chamadas) == 1)
                throw new InvalidOperationException("Base de dados indisponível (simulada).");

            SegundaVerificacao.TrySetResult(agora);
            return Task.FromResult(0);
        }
    }

    /// <summary>Cria o trabalhador da conclusão com o intervalo mínimo (1 s) e o serviço indicado.</summary>
    private static TrabalhadorConclusaoViagens CriarTrabalhadorConclusao(IConclusaoViagensService servico)
    {
        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConclusaoViagens:IntervaloSegundos"] = "1" })
            .Build();

        return new TrabalhadorConclusaoViagens(
            ScopesCom(servico),
            configuracao,
            NullLogger<TrabalhadorConclusaoViagens>.Instance);
    }

    /// <summary>
    /// Uma falha numa verificação fica no log e o temporizador continua: a verificação seguinte corre,
    /// com o «agora» em UTC.
    /// </summary>
    [Fact]
    public async Task I112_35_Conclusao_FalhaNumaVerificacao_NaoParaOTemporizador()
    {
        var servico = new ConclusaoQueFalhaAPrimeira();
        using TrabalhadorConclusaoViagens trabalhador = CriarTrabalhadorConclusao(servico);

        await trabalhador.StartAsync(CancellationToken.None);
        DateTime agora = await servico.SegundaVerificacao.Task.WaitAsync(AplicacaoReal.EsperaMaxima);
        await trabalhador.StopAsync(CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, agora.Kind);
        Assert.True(trabalhador.ExecuteTask!.IsCompletedSuccessfully);
    }

    /// <summary>Ao parar a aplicação antes da primeira verificação, o trabalhador termina e o serviço não é chamado.</summary>
    /// <remarks>
    /// Verifica-se <c>IsCompleted</c> e não «sem erro»: se a paragem chegar antes de o .NET arrancar o ciclo, a tarefa
    /// fica cancelada sem entrar no ciclo, o que também é uma paragem normal. A paragem a meio do ciclo, que tem de
    /// acabar sem erro, é verificada no I112_35.
    /// </remarks>
    [Fact]
    public async Task I112_36_Conclusao_ParagemAntesDaPrimeiraVerificacao_TerminaOTrabalhador()
    {
        var servico = new ConclusaoQueFalhaAPrimeira();
        using TrabalhadorConclusaoViagens trabalhador = CriarTrabalhadorConclusao(servico);

        await trabalhador.StartAsync(CancellationToken.None);
        await trabalhador.StopAsync(CancellationToken.None).WaitAsync(AplicacaoReal.EsperaMaxima);

        Assert.True(trabalhador.ExecuteTask!.IsCompleted);
        Assert.False(servico.SegundaVerificacao.Task.IsCompleted);
    }

    // ---------------------------------------------------------------- emails de confirmação

    /// <summary>
    /// Serviço de email falso: falha o envio para <see cref="DestinoComFalha"/> (como o Brevo a recusar) e
    /// regista os outros, avisando o teste pelo <see cref="EnvioRegistado"/>.
    /// </summary>
    private sealed class EmailQueFalhaUmDestino : IServicoEmail
    {
        public const string DestinoComFalha = "falha@estg.ipp.pt";

        public ConcurrentQueue<string> Enviados { get; } = new();
        public TaskCompletionSource EnvioRegistado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            if (destino == DestinoComFalha)
                throw new HttpRequestException("Brevo recusou (simulado).");

            Enviados.Enqueue(destino);
            EnvioRegistado.TrySetResult();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Serviço de email que nunca acaba: avisa pelo <see cref="Iniciado"/> e fica à espera até o
    /// <c>CancellationToken</c> ser cancelado (a aplicação a parar a meio de um envio).
    /// </summary>
    private sealed class EmailQueNuncaAcaba : IServicoEmail
    {
        public TaskCompletionSource Iniciado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            Iniciado.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
        }
    }

    /// <summary>Cria o trabalhador dos emails com a fila real e o serviço de email indicado.</summary>
    private static TrabalhadorEmails CriarTrabalhadorEmails(IFilaEmails fila, IServicoEmail email) =>
        new(fila, ScopesCom(email), NullLogger<TrabalhadorEmails>.Instance);

    /// <summary>Uma falha num envio fica no log (sem o endereço) e não impede o envio seguinte.</summary>
    [Fact]
    public async Task I112_37_Emails_FalhaNumEnvio_NaoParaOsSeguintes()
    {
        var fila = new FilaEmails(NullLogger<FilaEmails>.Instance);
        var email = new EmailQueFalhaUmDestino();
        using TrabalhadorEmails trabalhador = CriarTrabalhadorEmails(fila, email);

        await trabalhador.StartAsync(CancellationToken.None);
        await fila.EnfileirarConfirmacaoAsync(EmailQueFalhaUmDestino.DestinoComFalha, "token-1", CancellationToken.None);
        await fila.EnfileirarConfirmacaoAsync("bom@estg.ipp.pt", "token-2", CancellationToken.None);

        await email.EnvioRegistado.Task.WaitAsync(AplicacaoReal.EsperaMaxima);
        await trabalhador.StopAsync(CancellationToken.None);

        Assert.Equal(["bom@estg.ipp.pt"], email.Enviados);
    }

    /// <summary>Ao parar a aplicação a meio de um envio, o trabalhador desiste do envio e termina sem erro.</summary>
    [Fact]
    public async Task I112_38_Emails_ParagemAMeioDeUmEnvio_TerminaOTrabalhador()
    {
        var fila = new FilaEmails(NullLogger<FilaEmails>.Instance);
        var email = new EmailQueNuncaAcaba();
        using TrabalhadorEmails trabalhador = CriarTrabalhadorEmails(fila, email);

        await trabalhador.StartAsync(CancellationToken.None);
        await fila.EnfileirarConfirmacaoAsync("lento@estg.ipp.pt", "token", CancellationToken.None);
        await email.Iniciado.Task.WaitAsync(AplicacaoReal.EsperaMaxima);

        await trabalhador.StopAsync(CancellationToken.None).WaitAsync(AplicacaoReal.EsperaMaxima);

        Assert.True(trabalhador.ExecuteTask!.IsCompletedSuccessfully);
    }

    /// <summary>
    /// BE-39: com a fila de emails cheia e o trabalhador parado, enfileirar não espera; o email a mais é descartado
    /// e o primeiro continua na fila.
    /// </summary>
    [Fact]
    public async Task I112_55_Emails_FilaCheia_NaoPrendeORegisto()
    {
        var fila = new FilaEmails(NullLogger<FilaEmails>.Instance, capacidade: 1);

        await fila.EnfileirarConfirmacaoAsync("primeiro@estg.ipp.pt", "token-1", CancellationToken.None);
        Task segundo = fila.EnfileirarConfirmacaoAsync("segundo@estg.ipp.pt", "token-2", CancellationToken.None).AsTask();

        // Antes da correção, o segundo ficava à espera até haver espaço (aqui, para sempre).
        await segundo.WaitAsync(AplicacaoReal.EsperaMaxima);

        using var parar = new CancellationTokenSource();
        var lidos = new List<string>();
        await foreach (EmailConfirmacaoPendente lido in fila.LerPendentesAsync(parar.Token))
        {
            lidos.Add(lido.Destino);
            parar.Cancel();
            break;
        }

        Assert.Equal(["primeiro@estg.ipp.pt"], lidos);
    }
}
