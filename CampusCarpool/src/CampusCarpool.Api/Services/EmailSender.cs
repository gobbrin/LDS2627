
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CampusCarpool.Api.Services;

public sealed class EmailSender(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
{
    public async Task SendConfirmationAsync(
        string destination,
        string token,
        CancellationToken ct)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        var fromEmail = configuration["Brevo:FromEmail"];
        var fromName = configuration["Brevo:FromName"] ?? "Campus Carpool";
        var confirmationUrl = configuration["Frontend:ConfirmUrl"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(fromEmail) ||
            string.IsNullOrWhiteSpace(confirmationUrl))
        {
            throw new InvalidOperationException(
                "Brevo ou URL de confirmação não configurados.");
        }

        var separator = confirmationUrl.Contains('?') ? "&" : "?";
        var link = confirmationUrl + separator +
                   "token=" + Uri.EscapeDataString(token);

        var payload = new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new[] { new { email = destination } },
            subject = "Confirma a tua conta Campus Carpool",
            textContent =
                "Confirma o teu email através desta ligação " +
                "(válida por 24 horas): " + link
        };

        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Brevo recusou o envio. HTTP {(int)response.StatusCode}.");
        }
    }
}
