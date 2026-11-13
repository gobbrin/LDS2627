using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>
/// Cliente do Serviço de Mapas e Rotas: OpenRouteService alojado pela HeiGIT (<c>api.heigit.org</c>), perfil
/// <c>driving-car</c> (F008).
/// </summary>
/// <remarks>
/// Configuração: <c>Maps:ApiKey</c> (obrigatória; sem ela <see cref="Configurado"/> é falso e os serviços da
/// Application devolvem 503) e <c>Maps:TimeoutSeconds</c> (10 por omissão, no <c>HttpClient</c> registado em
/// <c>AddInfrastructureServices</c>).
/// Os URLs são absolutos e fixos, por isso <c>Maps:BaseUrl</c> e <c>Maps:Provider</c> não têm efeito (IN-04).
/// Erros HTTP e respostas sem o formato esperado saem como <see cref="HttpRequestException"/> e o tempo esgotado
/// como <see cref="TaskCanceledException"/>; <c>ChamadaMapas</c>, na Application, converte-os em 502 e 504.
/// </remarks>
public sealed class ServicoMapasRotas(HttpClient http, IConfiguration config) : IServicoMapasRotas
{
    private static readonly string[] MetricasMatriz = ["duration", "distance"];

    /// <summary>Indica se há chave da API configurada.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(config["Maps:ApiKey"]);

    /// <summary>Geocodificar uma localização: devolve o primeiro resultado do Pelias, ou <c>null</c> se não houver nenhum.</summary>
    /// <remarks>A chave vai na query string (exigência do Pelias), por isso pode aparecer em logs de pedidos HTTP.</remarks>
    /// <exception cref="InvalidOperationException">Sem <c>Maps:ApiKey</c>.</exception>
    public async Task<Coordenadas?> GeocodificarAsync(string texto, CancellationToken ct)
    {
        if (!Configurado) throw new InvalidOperationException("Maps:ApiKey não configurada.");

        var url = "https://api.heigit.org/pelias/v1/search?api_key=" +
    Uri.EscapeDataString(config["Maps:ApiKey"]!.Trim()) +
    "&size=1&text=" + Uri.EscapeDataString(texto);

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        return await LerRespostaAsync<Coordenadas?>(response, raiz =>
        {
            var features = raiz.GetProperty("features");

            if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() == 0) return null;

            // GeoJSON escreve [longitude, latitude], pela ordem inversa da habitual.
            var coordinates = features[0].GetProperty("geometry").GetProperty("coordinates");

            return new Coordenadas(coordinates[1].GetDecimal(), coordinates[0].GetDecimal());
        }, ct);
    }

    /// <summary>Calcular uma rota entre origem e destino (Directions API); <c>null</c> se o serviço não devolver rota.</summary>
    /// <exception cref="InvalidOperationException">Sem <c>Maps:ApiKey</c>.</exception>
    public async Task<ResultadoRota?> CalcularAsync(
        Coordenadas origem,
        Coordenadas destino,
        CancellationToken ct)
    {
        if (!Configurado)
            throw new InvalidOperationException("Maps:ApiKey não configurada.");

        var c = CultureInfo.InvariantCulture;

        var start = origem.Longitude.ToString(c) + "," +
                    origem.Latitude.ToString(c);

        var end = destino.Longitude.ToString(c) + "," +
                  destino.Latitude.ToString(c);

        var url = "https://api.heigit.org/openrouteservice/v2/directions/driving-car?start=" +
           start + "&end=" + end;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            config["Maps:ApiKey"]!.Trim());

        using var response = await http.SendAsync(request, ct);

        if (await SemRotaAsync(response, ct))
            return null;

        response.EnsureSuccessStatusCode();

        return await LerRespostaAsync<ResultadoRota?>(response, raiz =>
        {
            var features = raiz.GetProperty("features");

            if (features.ValueKind != JsonValueKind.Array ||
                features.GetArrayLength() == 0)
                return null;

            var summary = features[0]
                .GetProperty("properties")
                .GetProperty("summary");

            return new ResultadoRota(
                Math.Round(
                    summary.GetProperty("distance").GetDecimal() / 1000m,
                    2),
                (int)Math.Ceiling(
                    summary.GetProperty("duration").GetDecimal() / 60m),
                origem,
                destino);
        }, ct);
    }

    /// <summary>Calcular matriz de distâncias e durações entre todos os pares de pontos (Matrix API), num só pedido.</summary>
    /// <remarks>Usada na rota guardada e na simulação, para ordenar os pontos.</remarks>
    /// <exception cref="InvalidOperationException">Sem <c>Maps:ApiKey</c>.</exception>
    /// <exception cref="ArgumentException">Menos de dois pontos.</exception>
    public async Task<MatrizRotas?> CalcularMatrizAsync(
        IReadOnlyList<Coordenadas> pontos,
        CancellationToken ct)
    {
        var apiKey = config["Maps:ApiKey"]?.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Maps:ApiKey não configurada.");
        }

        if (pontos.Count < 2)
        {
            throw new ArgumentException(
                "São necessários pelo menos dois pontos.",
                nameof(pontos));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.heigit.org/openrouteservice/v2/matrix/driving-car");

        request.Headers.TryAddWithoutValidation("Authorization", apiKey);

        request.Content = JsonContent.Create(new
        {
            locations = pontos
                .Select(p => new[] { p.Longitude, p.Latitude })
                .ToArray(),
            metrics = MetricasMatriz
        });

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await LerRespostaAsync<MatrizRotas?>(response, raiz =>
        {
            double?[][] Ler(string propriedade) =>
                raiz.GetProperty(propriedade)
                    .EnumerateArray()
                    .Select(row => row.EnumerateArray()
                        .Select(v => v.ValueKind == JsonValueKind.Null
                            ? (double?)null
                            : v.GetDouble())
                        .ToArray())
                    .ToArray();

            return new MatrizRotas(
                Ler("durations"),
                Ler("distances"));
        }, ct);
    }

    /// <summary>Lê o JSON da resposta; se não tiver o formato esperado, conta como falha do serviço (RF-F008-12).</summary>
    private static async Task<T> LerRespostaAsync<T>(
        HttpResponseMessage response,
        Func<JsonElement, T> ler,
        CancellationToken ct)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct);

            return ler(json.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException
            or FormatException or IndexOutOfRangeException)
        {
            throw new HttpRequestException("Resposta do Serviço de Mapas sem o formato esperado.", ex);
        }
    }

    /// <summary>Indica se o serviço respondeu que não há rota entre os pontos.</summary>
    /// <remarks>
    /// O OpenRouteService usa 404 também para um endpoint indisponível, por isso só os códigos internos 2009 (sem rota)
    /// e 2010 (ponto sem estrada próxima) contam; qualquer outro 404 é uma falha do serviço.
    /// </remarks>
    private static async Task<bool> SemRotaAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode != HttpStatusCode.NotFound)
            return false;

        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("error", out var erro)
                && erro.ValueKind == JsonValueKind.Object
                && erro.TryGetProperty("code", out var codigo)
                && codigo.ValueKind == JsonValueKind.Number
                && codigo.TryGetInt32(out var numero)
                && numero is 2009 or 2010;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
