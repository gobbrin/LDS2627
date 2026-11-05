using System.Globalization;
using System.Text.Json;

namespace CampusCarpool.Api.Services;

/// <summary>Coordenadas geográficas em graus decimais (WGS 84).</summary>
public sealed record Coordenadas(
    decimal Latitude,
    decimal Longitude);

/// <summary>Rota entre dois pontos: distância em km (2 casas) e duração em minutos (arredondada para cima).</summary>
public sealed record ResultadoRota(
    decimal DistanciaKm,
    int DuracaoMinutos,
    Coordenadas Origem,
    Coordenadas Destino);

/// <summary>Matriz da Matrix API: [i][j] é a duração (s) ou distância (m) do ponto i ao j; <c>null</c> se não houver rota.</summary>
public sealed record MatrizRotas(
    double?[][] DuracoesSegundos,
    double?[][] DistanciasMetros);

/// <summary>
/// Cliente do Serviço de Mapas e Rotas: OpenRouteService alojado pela HeiGIT (<c>api.heigit.org</c>), perfil
/// <c>driving-car</c> (F008).
/// </summary>
/// <remarks>
/// Configuração: <c>Maps:ApiKey</c> (obrigatória; sem ela <see cref="Configurado"/> é falso e os controllers
/// respondem 503) e <c>Maps:TimeoutSeconds</c> (10 por omissão, no <c>HttpClient</c> do <c>Program.cs</c>).
/// Os URLs são absolutos e fixos, por isso <c>Maps:BaseUrl</c> e <c>Maps:Provider</c> não têm efeito (IN-04).
/// Erros HTTP saem como <see cref="HttpRequestException"/> (<c>EnsureSuccessStatusCode</c>) e o tempo esgotado
/// como <see cref="TaskCanceledException"/>; os controllers convertem-nos em 502 e 504.
/// </remarks>
public sealed class MapasService(HttpClient http, IConfiguration config)
{
    private static readonly string[] MetricasMatriz = ["duration", "distance"];

    /// <summary>Indica se há chave da API configurada.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(config["Maps:ApiKey"]);

    /// <summary>Geocodificar uma localização: devolve o primeiro resultado do Pelias, ou <c>null</c> se não houver nenhum.</summary>
    /// <remarks>
    /// A chave vai na query string (exigência do Pelias), por isso pode aparecer em logs de pedidos HTTP.
    /// A mensagem da exceção abaixo tem uma gralha de codificação ("nÃ£o"), a corrigir num commit de código.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Sem <c>Maps:ApiKey</c>.</exception>
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

        // GeoJSON escreve [longitude, latitude], pela ordem inversa da habitual.
        var coordinates = features[0].GetProperty("geometry").GetProperty("coordinates");

        return new Coordenadas(coordinates[1].GetDecimal(), coordinates[0].GetDecimal());
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

    /// <summary>Calcular uma rota com várias paragens, pela ordem dada, num só pedido.</summary>
    /// <remarks>Só é usado pelos testes; a rota da viagem usa a matriz (<see cref="CalcularMatrizAsync"/>).</remarks>
    /// <exception cref="InvalidOperationException">Sem <c>Maps:ApiKey</c>.</exception>
    /// <exception cref="ArgumentException">Menos de dois pontos.</exception>
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

    /// <summary>Calcular matriz de distâncias e durações entre todos os pares de pontos (Matrix API), num só pedido.</summary>
    /// <remarks>Usado pelo <c>OtimizadorRotas</c>, na rota guardada e na simulação.</remarks>
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
