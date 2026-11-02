using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusCarpool.Api.Services;

public sealed record Coordenadas(
    decimal Latitude,
    decimal Longitude);

public sealed record ResultadoRota(
    decimal DistanciaKm,
    int DuracaoMinutos,
    Coordenadas Origem,
    Coordenadas Destino);

public sealed record MatrizRotas(
    double?[][] DuracoesSegundos,
    double?[][] DistanciasMetros);

public sealed class MapasService(HttpClient http, IConfiguration config)
{
    public bool Configurado => !string.IsNullOrWhiteSpace(config["Maps:ApiKey"]);

    //Geocodificar uma localização.
    public async Task<Coordenadas?> GeocodificarAsync(string texto, CancellationToken ct)
    {
        if (!Configurado) throw new InvalidOperationException("Maps:ApiKey nÃ£o configurada.");

        var url = "https://api.heigit.org/pelias/v1/search?api_key=" +
    Uri.EscapeDataString(config["Maps:ApiKey"]!.Trim()) +
    "&size=1&text=" + Uri.EscapeDataString(texto);

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var features = json.RootElement.GetProperty("features");

        if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() == 0) return null;

        var coordinates = features[0].GetProperty("geometry").GetProperty("coordinates");

        return new Coordenadas(coordinates[1].GetDecimal(), coordinates[0].GetDecimal());
    }

    //Calcular uma rota entre origem e destino.
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
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var features = json.RootElement.GetProperty("features");

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
    }

    //Calcular uma rota com várias paragens.
    public async Task<ResultadoRota?> CalcularComParagensAsync(
        IReadOnlyList<Coordenadas> pontos,
        CancellationToken ct)
    {
        if (!Configurado)
            throw new InvalidOperationException(
                "Maps:ApiKey não configurada.");

        if (pontos.Count < 2)
            throw new ArgumentException(
                "São necessários pelo menos dois pontos.",
                nameof(pontos));

        var coordenadas = pontos
            .Select(p => new[] { p.Longitude, p.Latitude })
            .ToArray();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.heigit.org/openrouteservice/v2/directions/driving-car");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            config["Maps:ApiKey"]!.Trim());

        request.Content = JsonContent.Create(new
        {
            coordinates = coordenadas
        });

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var summary = json.RootElement
            .GetProperty("routes")[0]
            .GetProperty("summary");

        return new ResultadoRota(
            Math.Round(
                summary.GetProperty("distance").GetDecimal() / 1000m,
                2),
            (int)Math.Ceiling(
                summary.GetProperty("duration").GetDecimal() / 60m),
            pontos[0],
            pontos[^1]);
    }

    //Calcular matriz de distâncias e durações
    public async Task<MatrizRotas?> CalcularMatrizAsync(
        IReadOnlyList<Coordenadas> pontos,
        CancellationToken ct)
    {
        var apiKey = config["Maps:ApiKey"]?.Trim();

        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new InvalidOperationException(
                "Maps:ApiKey não configurada.");
        }

        if (pontos.Count < 2) {
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
            metrics = new[] { "duration", "distance" }
        });

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var root = json.RootElement;

        double?[][] Ler(string propriedade) =>
            root.GetProperty(propriedade)
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
    }

}