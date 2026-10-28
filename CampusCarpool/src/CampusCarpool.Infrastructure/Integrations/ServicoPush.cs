using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>Firebase Cloud Messaging HTTP v1; autenticação OAuth2 por service account.</summary>
public sealed class ServicoPush(HttpClient http, IConfiguration config,
    IRepositorioDispositivos dispositivos, ILogger<ServicoPush> logger) : IServicoPush
{
    private static string B64(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private async Task<string?> ObterAccessTokenAsync(CancellationToken ct)
    {
        var project = config["Firebase:ProjectId"];
        var email = config["Firebase:ClientEmail"];
        var key = config["Firebase:PrivateKey"]?.Replace("\\n", "\n");
        if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key))
            return null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = B64(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var claims = B64(JsonSerializer.SerializeToUtf8Bytes(new {
            iss = email, scope = "https://www.googleapis.com/auth/firebase.messaging",
            aud = "https://oauth2.googleapis.com/token", iat = now, exp = now + 3600
        }));
        var signingInput = header + "." + claims;
        using var rsa = RSA.Create();
        rsa.ImportFromPem(key);
        var signature = B64(rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        using var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string> {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = signingInput + "." + signature
            }), ct);
        tokenResponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("access_token").GetString();
    }
    public async Task EnviarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct)
    {
        var tokens = await dispositivos.ListarAtivosAsync(utilizadorId, ct);
        if (tokens.Count == 0) return;
        string? accessToken;
        try { accessToken = await ObterAccessTokenAsync(ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            logger.LogError(ex, "Falha ao autenticar Firebase Cloud Messaging"); return;
        }
        if (accessToken is null) { logger.LogWarning("Firebase não configurado; push não enviado"); return; }
        var project = config["Firebase:ProjectId"];
        foreach (var dispositivo in tokens)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(project!)}/messages:send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(JsonSerializer.Serialize(new {
                    message = new { token = dispositivo.Token,
                        notification = new { title = titulo, body = mensagem },
                        data = new Dictionary<string,string> { ["tipo"] = tipo },
                        android = new { priority = "HIGH" }
                    }
                }), Encoding.UTF8, "application/json");
                using var response = await http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode) continue;
                var error = await response.Content.ReadAsStringAsync(ct);
                // Desativar apenas tokens explicitamente inválidos/não registados.
                if (response.StatusCode == HttpStatusCode.NotFound ||
                    (response.StatusCode == HttpStatusCode.BadRequest && error.Contains("UNREGISTERED", StringComparison.Ordinal)))
                    await dispositivos.DesativarTokenInvalidoAsync(dispositivo.Token, ct);
                logger.LogWarning("FCM devolveu {Status} para dispositivo do utilizador {Id}: {Error}",
                    (int)response.StatusCode, utilizadorId, error.Length > 500 ? error[..500] : error);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Falha no envio push ao utilizador {Id}", utilizadorId); }
        }
    }
}
