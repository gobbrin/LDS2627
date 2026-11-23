using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>
/// Serviço de Mapas pela HERE, o fornecedor principal desde a D-5 (I112): geocodificação, rota direta com a linha e
/// matriz de tempos e distâncias (F008).
/// </summary>
/// <remarks>
/// <para>Configuração: <c>Here:ApiKey</c> (User Secrets ou a variável <c>Here__ApiKey</c>; nunca no repositório) e
/// <c>Here:TimeoutSeconds</c> (5 por omissão, no <c>HttpClient</c>): mais curto que o limite de 10 s da operação, para
/// sobrar tempo para a reserva (OpenRouteService) se a HERE não responder.</para>
/// <para>APIs (documentação oficial em <c>docs/.local-context/9-m4/16-here-ferramentas.md</c>):</para>
/// <list type="bullet">
/// <item>Geocodificação: <c>GET geocode.search.hereapi.com/v1/geocode?q=…&amp;limit=1&amp;in=countryCode:PRT</c>;
/// lê <c>items[0].position.lat/lng</c>.</item>
/// <item>Rota direta: <c>GET router.hereapi.com/v8/routes?transportMode=car&amp;return=summary,polyline</c>; lê
/// <c>sections[0].summary.length</c> (m), <c>duration</c> (s) e a <c>polyline</c> (a linha do mapa, no mesmo pedido).</item>
/// <item>Matriz: <c>POST matrix.router.hereapi.com/v8/matrix?async=false</c> com <c>regionDefinition</c>
/// <c>autoCircle</c> (modo «região»: até 500 × 500 pontos de forma síncrona, todos num círculo de 400 km de diâmetro).
/// A resposta traz listas planas: o par (i, j) está na posição <c>n × i + j</c>; um <c>errorCodes</c> diferente de 0
/// significa que não há rota para esse par.</item>
/// </list>
/// <para>Erros HTTP (incluindo 401, 403, 429 e 5xx) e respostas sem o formato esperado saem como
/// <see cref="HttpRequestException"/>; o tempo esgotado como <see cref="TaskCanceledException"/>. É o
/// <see cref="ServicoMapasComReserva"/> que decide passar à reserva.</para>
/// </remarks>
public sealed class ServicoMapasHere(HttpClient http, IConfiguration config) : IServicoMapasRotas
{
    private static readonly string[] AtributosDaMatriz = ["travelTimes", "distances"];

    /// <summary>Indica se há chave da HERE configurada.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(config["Here:ApiKey"]);

    /// <summary>Converte um local em coordenadas (HERE Geocoding &amp; Search); <c>null</c> se não o encontrar.</summary>
    /// <remarks>A pesquisa fica limitada a Portugal (<c>in=countryCode:PRT</c>): as viagens são entre campus e cidades portuguesas.</remarks>
    /// <exception cref="InvalidOperationException">Sem <c>Here:ApiKey</c>.</exception>
    public async Task<Coordenadas?> GeocodificarAsync(string texto, CancellationToken ct)
    {
        string url = "https://geocode.search.hereapi.com/v1/geocode?limit=1&in=countryCode:PRT&q="
            + Uri.EscapeDataString(texto)
            + "&apiKey=" + Uri.EscapeDataString(Chave());

        using HttpResponseMessage resposta = await http.GetAsync(url, ct);
        resposta.EnsureSuccessStatusCode();

        return await LeituraJson.LerAsync<Coordenadas?>(resposta, raiz =>
        {
            JsonElement itens = raiz.GetProperty("items");

            // Sem resultados, a HERE responde 200 com a lista vazia.
            if (itens.GetArrayLength() == 0)
                return null;

            JsonElement posicao = itens[0].GetProperty("position");
            return new Coordenadas(posicao.GetProperty("lat").GetDecimal(), posicao.GetProperty("lng").GetDecimal());
        }, ct);
    }

    /// <summary>Calcula a rota direta entre dois pontos (HERE Routing v8), com a linha do mapa; <c>null</c> se não houver rota.</summary>
    /// <exception cref="InvalidOperationException">Sem <c>Here:ApiKey</c>.</exception>
    public async Task<ResultadoRota?> CalcularAsync(Coordenadas origem, Coordenadas destino, CancellationToken ct)
    {
        string url = "https://router.hereapi.com/v8/routes?transportMode=car&return=summary,polyline"
            + "&origin=" + Texto(origem)
            + "&destination=" + Texto(destino)
            + "&apikey=" + Uri.EscapeDataString(Chave());

        using HttpResponseMessage resposta = await http.GetAsync(url, ct);
        resposta.EnsureSuccessStatusCode();

        return await LeituraJson.LerAsync<ResultadoRota?>(resposta, raiz =>
        {
            JsonElement rotas = raiz.GetProperty("routes");

            // Sem rota possível, a HERE responde 200 com a lista vazia.
            if (rotas.GetArrayLength() == 0)
                return null;

            // Sem paragens intermédias, a rota tem uma só secção.
            JsonElement seccao = rotas[0].GetProperty("sections")[0];
            JsonElement resumo = seccao.GetProperty("summary");
            string polilinha = seccao.GetProperty("polyline").GetString() ?? throw new FormatException("Secção sem linha.");

            return new ResultadoRota(
                Math.Round(resumo.GetProperty("length").GetDecimal() / 1000m, 2),
                (int)Math.Ceiling(resumo.GetProperty("duration").GetDecimal() / 60m),
                origem,
                destino,
                PolilinhaFlexivel.Descodificar(polilinha));
        }, ct);
    }

    /// <summary>Calcula as durações (s) e as distâncias (m) entre todos os pares de pontos (HERE Matrix Routing v8), num só pedido.</summary>
    /// <exception cref="InvalidOperationException">Sem <c>Here:ApiKey</c>.</exception>
    /// <exception cref="ArgumentException">Menos de dois pontos.</exception>
    public async Task<MatrizRotas?> CalcularMatrizAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pontos);

        if (pontos.Count < 2)
            throw new ArgumentException("São necessários pelo menos dois pontos.", nameof(pontos));

        // Os mesmos pontos são origens e destinos: a matriz é n × n.
        var locais = new List<object>();
        foreach (Coordenadas ponto in pontos)
            locais.Add(new { lat = ponto.Latitude, lng = ponto.Longitude });

        var corpo = new
        {
            origins = locais,
            destinations = locais,
            regionDefinition = new { type = "autoCircle" },
            matrixAttributes = AtributosDaMatriz,
            transportMode = "car"
        };

        string url = "https://matrix.router.hereapi.com/v8/matrix?async=false&apikey=" + Uri.EscapeDataString(Chave());

        using HttpResponseMessage resposta = await http.PostAsync(url, JsonContent.Create(corpo), ct);
        resposta.EnsureSuccessStatusCode();

        return await LeituraJson.LerAsync<MatrizRotas?>(resposta, raiz => LerMatriz(raiz.GetProperty("matrix"), pontos.Count), ct);
    }

    /// <summary>Converte as listas planas da resposta numa matriz n × n; um par com erro fica a <c>null</c> (sem rota).</summary>
    private static MatrizRotas LerMatriz(JsonElement matriz, int n)
    {
        JsonElement tempos = matriz.GetProperty("travelTimes");
        JsonElement distancias = matriz.GetProperty("distances");
        bool temErros = matriz.TryGetProperty("errorCodes", out JsonElement erros);

        var duracoesSegundos = new double?[n][];
        var distanciasMetros = new double?[n][];

        for (int i = 0; i < n; i++)
        {
            duracoesSegundos[i] = new double?[n];
            distanciasMetros[i] = new double?[n];

            for (int j = 0; j < n; j++)
            {
                // A HERE devolve a matriz numa lista plana, linha a linha.
                int posicao = n * i + j;

                if (temErros && erros[posicao].GetInt32() != 0)
                    continue;

                duracoesSegundos[i][j] = tempos[posicao].GetDouble();
                distanciasMetros[i][j] = distancias[posicao].GetDouble();
            }
        }

        return new MatrizRotas(duracoesSegundos, distanciasMetros);
    }

    /// <summary>Chave da HERE, sem espaços nas pontas.</summary>
    /// <exception cref="InvalidOperationException">Sem <c>Here:ApiKey</c>.</exception>
    private string Chave()
    {
        if (!Configurado)
            throw new InvalidOperationException("Here:ApiKey não configurada.");

        return config["Here:ApiKey"]!.Trim();
    }

    /// <summary>Coordenadas no formato da HERE: «latitude,longitude», com ponto decimal e até 6 casas.</summary>
    private static string Texto(Coordenadas ponto) =>
        ponto.Latitude.ToString("0.######", CultureInfo.InvariantCulture) + ","
        + ponto.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
}
