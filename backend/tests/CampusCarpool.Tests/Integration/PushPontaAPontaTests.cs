using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Infrastructure.BackgroundJobs;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// O push sai de ponta a ponta (RF-F006-11, P-24; BE-01, R-B08, R-B09, I112): pedido HTTP → notificação → fila real
/// → <see cref="TrabalhadorNotificacoesPush"/> real → <see cref="ServicoPush"/> real → pedido ao FCM.
/// </summary>
/// <remarks>
/// Só o FCM e o OAuth da Google são falsos (<see cref="FcmFalso"/>); a chave RSA da service account é gerada no teste.
/// Os outros testes trocam a fila por <see cref="FilaNotificacoesPushFalsa"/> e nunca chegam ao trabalhador.
/// </remarks>
public sealed class PushPontaAPontaTests
{
    private const string TokenDoCondutor = "token-fcm-do-condutor";

    /// <summary>Responde ao OAuth e ao FCM; guarda o corpo de cada envio ao FCM.</summary>
    private sealed class FcmFalso : HttpMessageHandler
    {
        public ConcurrentQueue<string> Envios { get; } = new();

        // Concluída no primeiro envio ao FCM, para o teste esperar sem ciclos de espera.
        public TaskCompletionSource PrimeiroEnvio { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.Host == "oauth2.googleapis.com")
                return Json("""{"access_token":"token-oauth-de-teste","expires_in":3600}""");

            Envios.Enqueue(request.RequestUri + " " + await request.Content!.ReadAsStringAsync(ct));
            PrimeiroEnvio.TrySetResult();

            return Json("""{"name":"projects/projeto-teste/messages/1"}""");
        }

        private static HttpResponseMessage Json(string texto) =>
            new(HttpStatusCode.OK) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };
    }

    /// <summary>Configuração do Firebase com dados fictícios e uma chave RSA nova.</summary>
    private static IConfiguration FirebaseFicticio()
    {
        using var rsa = RSA.Create(2048);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firebase:ProjectId"] = "projeto-teste",
                ["Firebase:ClientEmail"] = "teste@example.invalid",
                ["Firebase:PrivateKey"] = rsa.ExportPkcs8PrivateKeyPem()
            })
            .Build();
    }

    [Fact]
    /// <summary>Um pedido de participação faz sair o push «Pedido recebido» para o dispositivo do condutor.</summary>
    public async Task I112_12_PedidoDeParticipacao_EnviaOPushAoCondutorPeloFcm()
    {
        var fcm = new FcmFalso();
        IConfiguration firebase = FirebaseFicticio();

        using var app = new AplicacaoReal(services =>
        {
            // A fila real, no lugar da falsa que a AplicacaoReal põe por omissão.
            services.RemoveAll<IFilaNotificacoesPush>();
            services.AddSingleton<IFilaNotificacoesPush>(new FilaNotificacoesPush(NullLogger<FilaNotificacoesPush>.Instance));

            // O ServicoPush real, com o FCM falso e o Firebase fictício.
            services.RemoveAll<IServicoPush>();
            services.AddScoped<IServicoPush>(sp => new ServicoPush(
                new HttpClient(fcm, disposeHandler: false),
                firebase,
                sp.GetRequiredService<IRepositorioDispositivos>(),
                NullLogger<ServicoPush>.Instance,
                sp.GetRequiredService<CacheTokenFcm>()));
        });

        var condutor = app.CriarUtilizador("Condutor");
        var passageiro = app.CriarUtilizador("Passageiro");
        using var clienteCondutor = app.ClienteDe(condutor);
        using var clientePassageiro = app.ClienteDe(passageiro);

        using var registo = await clienteCondutor.PostAsJsonAsync("/api/v1/dispositivos", new { token = TokenDoCondutor });
        Assert.Equal(HttpStatusCode.NoContent, registo.StatusCode);

        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);
        await AplicacaoReal.PedirParticipacaoAsync(clientePassageiro, viagemId);

        // Espera pelo envio com o limite generoso dos testes (R1): 5 s não chegavam com a suite toda em SQL Server.
        // O tempo de entrega de P-24 (30 s) não se mede aqui, porque depende da carga da máquina de testes.
        await fcm.PrimeiroEnvio.Task.WaitAsync(AplicacaoReal.EsperaMaxima);

        string envio = Assert.Single(fcm.Envios);
        Assert.StartsWith("https://fcm.googleapis.com/v1/projects/projeto-teste/messages:send ", envio);

        using var corpo = JsonDocument.Parse(envio[(envio.IndexOf(' ') + 1)..]);
        JsonElement mensagem = corpo.RootElement.GetProperty("message");
        Assert.Equal(TokenDoCondutor, mensagem.GetProperty("token").GetString());
        Assert.Equal("Pedido recebido", mensagem.GetProperty("notification").GetProperty("title").GetString());
        JsonElement dados = mensagem.GetProperty("data");
        Assert.Equal("PedidoRecebido", dados.GetProperty("tipo").GetString());

        // BE-17: o push diz a que viagem e a que notificação se refere, como texto.
        Assert.Equal(viagemId.ToString("D"), dados.GetProperty("viagemId").GetString());
        Guid notificacaoId = Guid.Parse(dados.GetProperty("notificacaoId").GetString()!);
        Assert.True(app.NaBaseDeDados(db => db.Notificacoes.Any(n => n.Id == notificacaoId && n.UtilizadorId == condutor.Id)));
    }

    /// <summary>Serviço de push falso para o trabalhador: o primeiro envio falha, os seguintes ficam registados.</summary>
    private sealed class PushQueFalhaAPrimeira : IServicoPush
    {
        private int _chamadas;

        public ConcurrentQueue<string> Enviados { get; } = new();
        public TaskCompletionSource SegundoEnvio { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task EnviarAsync(NotificacaoPushPendente notificacao, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _chamadas) == 1)
                throw new HttpRequestException("Falha simulada no FCM.");

            Enviados.Enqueue(notificacao.Titulo);
            SegundoEnvio.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private static TrabalhadorNotificacoesPush CriarTrabalhador(IFilaNotificacoesPush fila, IServicoPush push)
    {
        ServiceProvider servicos = new ServiceCollection().AddSingleton(push).BuildServiceProvider();

        return new TrabalhadorNotificacoesPush(
            fila,
            servicos.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TrabalhadorNotificacoesPush>.Instance);
    }

    [Fact]
    /// <summary>Uma falha num envio fica no log e não impede os envios seguintes.</summary>
    public async Task I112_13_FalhaNumEnvio_NaoParaOsSeguintes()
    {
        var fila = new FilaNotificacoesPush(NullLogger<FilaNotificacoesPush>.Instance);
        var push = new PushQueFalhaAPrimeira();
        using var trabalhador = CriarTrabalhador(fila, push);

        await trabalhador.StartAsync(CancellationToken.None);
        await fila.EnfileirarAsync(Pendente("Primeiro"), CancellationToken.None);
        await fila.EnfileirarAsync(Pendente("Segundo"), CancellationToken.None);

        await push.SegundoEnvio.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await trabalhador.StopAsync(CancellationToken.None);

        Assert.Equal(["Segundo"], push.Enviados);
    }

    [Fact]
    /// <summary>Ao parar a aplicação, o trabalhador sai do ciclo mesmo com a fila vazia.</summary>
    public async Task I112_14_Paragem_TerminaOTrabalhador()
    {
        using var trabalhador = CriarTrabalhador(new FilaNotificacoesPush(NullLogger<FilaNotificacoesPush>.Instance), new PushQueFalhaAPrimeira());

        await trabalhador.StartAsync(CancellationToken.None);
        await trabalhador.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(trabalhador.ExecuteTask!.IsCompleted);
    }

    /// <summary>Notificação de teste com o título indicado.</summary>
    private static NotificacaoPushPendente Pendente(string titulo) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), titulo, "Texto", "PedidoRecebido");

    [Fact]
    /// <summary>BE-28: com a fila cheia e o trabalhador parado, enfileirar não espera; a notificação a mais é descartada.</summary>
    public async Task I112_15_FilaCheia_NaoPrendeQuemEnfileira()
    {
        var fila = new FilaNotificacoesPush(NullLogger<FilaNotificacoesPush>.Instance, capacidade: 1);

        await fila.EnfileirarAsync(Pendente("Primeiro"), CancellationToken.None);
        Task segunda = fila.EnfileirarAsync(Pendente("Segundo"), CancellationToken.None).AsTask();

        // Antes da correção, o segundo ficava à espera até haver espaço (aqui, para sempre).
        await segunda.WaitAsync(TimeSpan.FromSeconds(1));

        using var parar = new CancellationTokenSource();
        var lidas = new List<string>();
        await foreach (NotificacaoPushPendente lida in fila.LerPendentesAsync(parar.Token))
        {
            lidas.Add(lida.Titulo);
            parar.Cancel();
            break;
        }

        Assert.Equal(["Primeiro"], lidas);
    }
}
