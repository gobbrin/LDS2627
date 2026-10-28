using System.Net.Http;
using System.Net.Http.Json;
using CampusCarpool.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>Implementa o envio de emails de confirmação através da Brevo.</summary>
/// <remarks>
/// Esta classe contém o detalhe externo da integração. A API depende apenas de
/// <see cref="IServicoEmail"/> e os testes substituem-na por um fake.
/// </remarks>
public sealed class ServicoEmail(
	IConfiguration configuration,
	HttpClient client) : IServicoEmail
{
	// A API conhece apenas IServicoEmail; Brevo fica isolado nesta integração.
	// Um fornecedor diferente pode implementar o mesmo contrato sem alterar controllers.
	/// <summary>Envia o email de confirmação através da API Brevo.</summary>
	public async Task EnviarConfirmacaoAsync(
		string destino,
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
			to = new[] { new { email = destino } },
			subject = "Confirma a tua conta Campus Carpool",
			textContent =
				"Confirma o teu email através desta ligação " +
				"(válida por 24 horas): " + link
		};

		using var request = new HttpRequestMessage(
			HttpMethod.Post,
			"https://api.brevo.com/v3/smtp/email");

		request.Headers.Add("api-key", apiKey);
		request.Content = JsonContent.Create(payload);

		// O envio acontece fora da transação de registo; falhas de email não devem
		// deixar o controller dependente de um fornecedor externo.
		using var response = await client.SendAsync(request, ct);
		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException(
				$"Brevo recusou o envio. HTTP {(int)response.StatusCode}.");
		}
	}
}
