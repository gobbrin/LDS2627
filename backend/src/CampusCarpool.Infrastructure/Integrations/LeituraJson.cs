using System.Text.Json;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>Leitura das respostas JSON dos fornecedores de mapas (HERE e OpenRouteService).</summary>
internal static class LeituraJson
{
    /// <summary>Lê o JSON da resposta; se não tiver o formato esperado, conta como falha do serviço (RF-F008-12).</summary>
    /// <typeparam name="T">Tipo do valor lido.</typeparam>
    /// <param name="resposta">Resposta HTTP com sucesso.</param>
    /// <param name="ler">Função que tira o valor da raiz do JSON.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>O valor lido.</returns>
    /// <exception cref="HttpRequestException">JSON inválido ou sem os campos esperados (o chamador trata como 502).</exception>
    public static async Task<T> LerAsync<T>(HttpResponseMessage resposta, Func<JsonElement, T> ler, CancellationToken ct)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(
                await resposta.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct);

            return ler(json.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException
            or FormatException or IndexOutOfRangeException)
        {
            throw new HttpRequestException("Resposta do Serviço de Mapas sem o formato esperado.", ex);
        }
    }
}
