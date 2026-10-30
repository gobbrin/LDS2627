using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class ServicoPushTests
{
    [Fact]
    public async Task EnviarAsync_SemDispositivosAtivos_NaoFazPedidosHttp()
    {
        var repositorio = new RepositorioDispositivosFalso();
        var pedidosHttp = 0;

        using var http = new HttpClient(new HandlerHttpFalso(
            (_, _) =>
            {
                pedidosHttp++;
                throw new InvalidOperationException(
                    "Não deveria ser efetuado qualquer pedido HTTP.");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(Guid.NewGuid(), "Título", "Mensagem", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(1, repositorio.ListarChamadas);
        Assert.Equal(0, pedidosHttp);
        Assert.Equal(0, repositorio.Desativacoes);
    }

    [Fact]
    public async Task EnviarAsync_ComSucesso_EnviaMensagemFcm()
    {
        var utilizadorId = Guid.NewGuid();
        var tokenDispositivo = "token-teste";
        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = tokenDispositivo,
                    Ativo = true
                }
            ]
        };

        var pedidosFcm = 0;

        using var http = new HttpClient(new HandlerHttpFalso(
            (pedido, _) =>
            {
                if (pedido.RequestUri!.Host == "oauth2.googleapis.com")
                {
                    return Resposta(HttpStatusCode.OK,
                        """{"access_token":"token-oauth-teste"}""");
                }

                pedidosFcm++;

                Assert.Equal(HttpMethod.Post, pedido.Method);
                Assert.Contains("/v1/projects/projeto-teste/messages:send", pedido.RequestUri!.AbsolutePath);

                Assert.Equal("Bearer", pedido.Headers.Authorization?.Scheme);

                Assert.Equal("token-oauth-teste", pedido.Headers.Authorization?.Parameter);

                return Resposta(HttpStatusCode.OK, """{"name":"mensagem-teste"}""");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(utilizadorId, "Novo pedido", "Recebeste um pedido.", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(1, pedidosFcm);
        Assert.Equal(0, repositorio.Desativacoes);
    }

    [Fact]
    public async Task EnviarAsync_ComTokenNaoRegistado_DesativaTokenInvalido()
    {
        var utilizadorId = Guid.NewGuid();
        const string tokenDispositivo = "token-invalido";

        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = tokenDispositivo,
                    Ativo = true
                }
            ]
        };

        using var http = new HttpClient(new HandlerHttpFalso(
            (pedido, _) =>
            {
                if (pedido.RequestUri!.Host == "oauth2.googleapis.com")
                {
                    return Resposta(HttpStatusCode.OK, """{"access_token":"token-oauth-teste"}""");
                }

                return Resposta(HttpStatusCode.NotFound, """{"error":{"status":"NOT_FOUND"}}""");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(utilizadorId, "Título", "Mensagem", "PedidoAceite", CancellationToken.None);

        Assert.Equal(1, repositorio.Desativacoes);
        Assert.Equal(tokenDispositivo, repositorio.UltimoTokenDesativado);
    }

    [Fact]
    public async Task EnviarAsync_ComFirebaseNaoConfigurado_NaoEnviaPush()
    {
        var utilizadorId = Guid.NewGuid();

        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = "token-teste",
                    Ativo = true
                }
            ]
        };

        using var http = new HttpClient(new HandlerHttpFalso(
            (_, _) => throw new InvalidOperationException("Não deveria ser efetuado qualquer pedido HTTP.")));

        var servico = new ServicoPush(http, new ConfigurationBuilder().Build(), repositorio, NullLogger<ServicoPush>.Instance);

        await servico.EnviarAsync(utilizadorId, "Título", "Mensagem", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(0, repositorio.Desativacoes);
    }

    
    [Fact]
    public async Task EnviarAsync_ComErroUnregistered_DesativaToken()
    {
        var utilizadorId = Guid.NewGuid();
        const string tokenDispositivo = "token-unregistered";

        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = tokenDispositivo,
                    Ativo = true
                }
            ]
        };

        using var http = new HttpClient(new HandlerHttpFalso(
            (pedido, _) =>
            {
                if (pedido.RequestUri!.Host == "oauth2.googleapis.com")
                {
                    return Resposta(HttpStatusCode.OK, """{"access_token":"token-oauth-teste"}""");
                }

                return Resposta(HttpStatusCode.BadRequest, """{"error":{"status":"UNREGISTERED"}}""");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(utilizadorId, "Título", "Mensagem", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(1, repositorio.Desativacoes);
        Assert.Equal(tokenDispositivo, repositorio.UltimoTokenDesativado);
    }

    [Fact]
    public async Task EnviarAsync_ComErro500_NaoDesativaToken()
    {
        var utilizadorId = Guid.NewGuid();
        const string tokenDispositivo = "token-temporariamente-indisponivel";

        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = tokenDispositivo,
                    Ativo = true
                }
            ]
        };

        using var http = new HttpClient(new HandlerHttpFalso(
            (pedido, _) =>
            {
                if (pedido.RequestUri!.Host == "oauth2.googleapis.com")
                {
                    return Resposta(HttpStatusCode.OK, """{"access_token":"token-oauth-teste"}""");
                }

                return Resposta(HttpStatusCode.InternalServerError, """{"error":{"status":"INTERNAL"}}""");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(utilizadorId, "Título", "Mensagem", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(0, repositorio.Desativacoes);
    }

    [Fact]
    public async Task EnviarAsync_ComErroNumPrimeiroDispositivo_ContinuaParaOSeguinte()
    {
        var utilizadorId = Guid.NewGuid();

        var repositorio = new RepositorioDispositivosFalso
        {
            Dispositivos =
            [
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = "token-primeiro",
                    Ativo = true
                },
                new Dispositivo
                {
                    UtilizadorId = utilizadorId,
                    Token = "token-segundo",
                    Ativo = true
                }
            ]
        };

        var pedidosFcm = 0;

        using var http = new HttpClient(new HandlerHttpFalso(
            (pedido, _) =>
            {
                if (pedido.RequestUri!.Host == "oauth2.googleapis.com")
                {
                    return Resposta(HttpStatusCode.OK, """{"access_token":"token-oauth-teste"}""");
                }

                pedidosFcm++;

                var corpo = pedido.Content!
                    .ReadAsStringAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                using var documento = JsonDocument.Parse(corpo);
                var token = documento.RootElement
                    .GetProperty("message")
                    .GetProperty("token")
                    .GetString();

                if (token == "token-primeiro")
                {
                    return Resposta(HttpStatusCode.InternalServerError, """{"error":{"status":"INTERNAL"}}""");
                }

                return Resposta(HttpStatusCode.OK, """{"name":"mensagem-teste"}""");
            }
        ));

        var servico = CriarServico(http, repositorio);

        await servico.EnviarAsync(utilizadorId, "Título", "Mensagem", "PedidoRecebido", CancellationToken.None);

        Assert.Equal(2, pedidosFcm);
        Assert.Equal(0, repositorio.Desativacoes);
    }

    private static ServicoPush CriarServico(HttpClient http, RepositorioDispositivosFalso repositorio)
    {
        using var rsa = RSA.Create(2048);

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firebase:ProjectId"] = "projeto-teste",
                ["Firebase:ClientEmail"] = "teste@example.invalid",
                ["Firebase:PrivateKey"] = rsa.ExportPkcs8PrivateKeyPem()
            })
            .Build();

        return new ServicoPush(http, configuracao, repositorio, NullLogger<ServicoPush>.Instance);
    }

    private static HttpResponseMessage Resposta(HttpStatusCode status, string conteudo)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(
                conteudo,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class HandlerHttpFalso(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request, cancellationToken));
        }
    }

    private sealed class RepositorioDispositivosFalso : IRepositorioDispositivos
    {
        public IReadOnlyList<Dispositivo> Dispositivos { get; init; } = [];

        public int ListarChamadas { get; private set; }

        public int Desativacoes { get; private set; }

        public string? UltimoTokenDesativado { get; private set; }

        public Task<IReadOnlyList<Dispositivo>> ListarAtivosAsync(Guid utilizadorId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ListarChamadas++;

            IReadOnlyList<Dispositivo> resultado = Dispositivos
                .Where(d => d.UtilizadorId == utilizadorId && d.Ativo)
                .ToList();

            return Task.FromResult(resultado);
        }

        public Task RegistarAsync(Guid utilizadorId, string token, CancellationToken ct) => Task.CompletedTask;

        public Task DesativarAsync(Guid utilizadorId, string token, CancellationToken ct) => Task.CompletedTask;

        public Task DesativarTokenInvalidoAsync(string token, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Desativacoes++;
            UltimoTokenDesativado = token;
            return Task.CompletedTask;
        }
    }
}
