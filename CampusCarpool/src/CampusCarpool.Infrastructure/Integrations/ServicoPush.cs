using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>Firebase Cloud Messaging HTTP v1; autenticação OAuth2 por service account.</summary>
/// <remarks>
/// Configuração: <c>Firebase:ProjectId</c>, <c>Firebase:ClientEmail</c> e <c>Firebase:PrivateKey</c> (PEM; as
/// quebras de linha podem vir escritas como <c>\n</c>). Sem configuração, o push é ignorado com um aviso no log.
/// Os erros nunca chegam ao pedido HTTP do utilizador: o envio corre no <c>TrabalhadorNotificacoesPush</c>.
/// O token OAuth é reutilizado enquanto for válido (<see cref="CacheTokenFcm"/>, IN-05).
/// </remarks>
public sealed class ServicoPush(HttpClient http, IConfiguration config,
    IRepositorioDispositivos dispositivos, ILogger<ServicoPush> logger, CacheTokenFcm cacheToken) : IServicoPush
{
    // A Google indica a validade na resposta; este valor só se usa se não vier.
    private const int ValidadeTokenSegundos = 3600;

    private static readonly Action<ILogger, Exception?> LogFalhaAutenticacao = LoggerMessage.Define(
        LogLevel.Error, new EventId(2001, nameof(LogFalhaAutenticacao)), "Falha ao autenticar Firebase Cloud Messaging");

    private static readonly Action<ILogger, Exception?> LogNaoConfigurado = LoggerMessage.Define(
        LogLevel.Warning, new EventId(2002, nameof(LogNaoConfigurado)), "Firebase não configurado; push não enviado");

    private static readonly Action<ILogger, int, Guid, string, Exception?> LogRespostaFcm = LoggerMessage.Define<int, Guid, string>(
        LogLevel.Warning, new EventId(2003, nameof(LogRespostaFcm)), "FCM devolveu {Status} para dispositivo do utilizador {Id}: {Error}");

    private static readonly Action<ILogger, Guid, Exception?> LogFalhaEnvio = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(2004, nameof(LogFalhaEnvio)), "Falha no envio push ao utilizador {Id}");

    // Base64url sem "=" (formato exigido pelo JWT).
    private static string B64(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // Fluxo OAuth2 "JWT bearer" da Google: assina um JWT RS256 com a chave privada da service account e troca-o
    // por um access token. Devolve null se o Firebase não estiver configurado.
    private async Task<string?> ObterAccessTokenAsync(CancellationToken ct)
    {
        var project = config["Firebase:ProjectId"];
        var email = config["Firebase:ClientEmail"];
        var key = config["Firebase:PrivateKey"]?.Replace("\\n", "\n");
        if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key))
            return null;

        var agora = DateTime.UtcNow;

        if (cacheToken.Obter(agora) is { } guardado)
            return guardado;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = B64(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var claims = B64(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = email,
            scope = "https://www.googleapis.com/auth/firebase.messaging",
            aud = "https://oauth2.googleapis.com/token",
            iat = now,
            exp = now + 3600
        }));
        var signingInput = header + "." + claims;
        using var rsa = RSA.Create();
        rsa.ImportFromPem(key);
        var signature = B64(rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        using var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = signingInput + "." + signature
            }), ct);
        tokenResponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        var accessToken = doc.RootElement.GetProperty("access_token").GetString();

        if (accessToken is not null)
        {
            var validade = doc.RootElement.TryGetProperty("expires_in", out var segundos) ? segundos.GetInt32() : ValidadeTokenSegundos;
            cacheToken.Guardar(accessToken, TimeSpan.FromSeconds(validade), agora);
        }

        return accessToken;
    }
    private async Task<HttpResponseMessage> EnviarAoFcmAsync(string project, string accessToken, string conteudo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(project)}/messages:send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(conteudo, Encoding.UTF8, "application/json");
        return await http.SendAsync(request, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Um pedido ao FCM por dispositivo ativo. Os tokens que o FCM declara inválidos (404 ou
    /// <c>UNREGISTERED</c>) são desativados; as outras falhas só ficam no log.
    /// </remarks>
    public async Task EnviarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct)
    {
        var tokens = await dispositivos.ListarAtivosAsync(utilizadorId, ct);
        if (tokens.Count == 0) return;
        string? accessToken;
        try { accessToken = await ObterAccessTokenAsync(ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFalhaAutenticacao(logger, ex); return;
        }
        if (accessToken is null) { LogNaoConfigurado(logger, null); return; }
        var project = config["Firebase:ProjectId"];
        foreach (var dispositivo in tokens)
        {
            try
            {
                var conteudo = JsonSerializer.Serialize(new
                {
                    message = new
                    {
                        token = dispositivo.Token,
                        notification = new { title = titulo, body = mensagem },
                        data = new Dictionary<string, string> { ["tipo"] = tipo },
                        android = new { priority = "HIGH" }
                    }
                });
                var resposta = await EnviarAoFcmAsync(project!, accessToken, conteudo, ct);

                // Token revogado antes do tempo: pede outro e tenta uma vez, para não perder a notificação.
                if (resposta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    cacheToken.Limpar();

                    if (await ObterAccessTokenAsync(ct) is { } novo)
                    {
                        accessToken = novo;
                        resposta.Dispose();
                        resposta = await EnviarAoFcmAsync(project!, accessToken, conteudo, ct);
                    }
                }

                using var response = resposta;
                if (response.IsSuccessStatusCode) continue;
                var error = await response.Content.ReadAsStringAsync(ct);
                // Desativar apenas tokens explicitamente inválidos/não registados.
                if (response.StatusCode == HttpStatusCode.NotFound ||
                    (response.StatusCode == HttpStatusCode.BadRequest && error.Contains("UNREGISTERED", StringComparison.Ordinal)))
                    await dispositivos.DesativarTokenInvalidoAsync(dispositivo.Token, ct);
                LogRespostaFcm(logger, (int)response.StatusCode, utilizadorId, error.Length > 500 ? error[..500] : error, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { LogFalhaEnvio(logger, utilizadorId, ex); }
        }
    }
}
