using System.Globalization;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>
/// Linha da rota pelo HERE Routing v8 (<c>router.hereapi.com/v8/routes</c>), para desenhar no mapa (RF-F008-07,
/// DEF-BE-054).
/// </summary>
/// <remarks>
/// <para>Configuração: <c>Here:ApiKey</c> (User Secrets ou a variável <c>Here__ApiKey</c>; nunca no repositório).
/// Sem ela, <see cref="Configurado"/> é falso e a Application usa linhas retas entre os pontos.</para>
/// <para>Pedido: <c>origin</c>, <c>destination</c> e um <c>via</c> por paragem (recolha ou destino de um passageiro),
/// <c>transportMode=car</c> e <c>return=polyline</c>. A resposta traz uma secção por troço entre paragens; as linhas
/// das secções juntam-se numa só. A chave vai na query string (<c>apikey</c>), como a HERE exige.</para>
/// <para>Qualquer falha (erro HTTP, tempo esgotado, resposta sem o formato esperado) fica num aviso no log e devolve
/// <c>null</c>: a falta da linha nunca impede uma operação.</para>
/// </remarks>
public sealed class ServicoGeometriaHere(
    HttpClient http,
    IConfiguration config,
    ILogger<ServicoGeometriaHere> logger) : IServicoGeometriaRotas
{
    private const string Endereco = "https://router.hereapi.com/v8/routes";

    // Um só aviso para todas as falhas; o motivo diz qual foi.
    private static readonly Action<ILogger, string, Exception?> LogSemLinha = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(8101, nameof(LogSemLinha)),
        "HERE Routing sem linha ({Motivo}); a rota fica com linhas retas.");

    /// <summary>Indica se há chave da HERE configurada.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(config["Here:ApiKey"]);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Coordenadas>?> CalcularLinhaAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pontos);

        if (!Configurado)
            return null;

        List<Coordenadas> paragens = SemRepetidosSeguidos(pontos);
        if (paragens.Count < 2)
            return null;

        try
        {
            using var resposta = await http.GetAsync(MontarEndereco(paragens), ct);

            if (!resposta.IsSuccessStatusCode)
            {
                LogSemLinha(logger, $"respondeu {(int)resposta.StatusCode}", null);
                return null;
            }

            string json = await resposta.Content.ReadAsStringAsync(ct);
            return LerLinha(json);
        }
        catch (HttpRequestException ex)
        {
            LogSemLinha(logger, "serviço indisponível", ex);
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // O HttpClient esgotou o tempo; o pedido do cliente continua.
            LogSemLinha(logger, "tempo esgotado", null);
            return null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            LogSemLinha(logger, "resposta sem o formato esperado", ex);
            return null;
        }
    }

    /// <summary>Monta o URL do pedido: origem, paragens intermédias (<c>via</c>) e destino.</summary>
    private string MontarEndereco(List<Coordenadas> paragens)
    {
        var url = new StringBuilder(Endereco);
        url.Append("?transportMode=car&return=polyline");
        url.Append("&origin=").Append(Texto(paragens[0]));

        // Cada ponto intermédio é uma paragem: a HERE cria uma secção por troço.
        for (int i = 1; i < paragens.Count - 1; i++)
            url.Append("&via=").Append(Texto(paragens[i]));

        url.Append("&destination=").Append(Texto(paragens[^1]));
        url.Append("&apikey=").Append(Uri.EscapeDataString(config["Here:ApiKey"]!.Trim()));

        return url.ToString();
    }

    /// <summary>Lê a resposta da HERE e junta as linhas de todas as secções; <c>null</c> se não houver rota.</summary>
    private static List<Coordenadas>? LerLinha(string json)
    {
        using var documento = JsonDocument.Parse(json);
        JsonElement rotas = documento.RootElement.GetProperty("routes");

        // Sem rota possível, a HERE responde 200 com a lista vazia.
        if (rotas.GetArrayLength() == 0)
            return null;

        var linha = new List<Coordenadas>();

        foreach (JsonElement seccao in rotas[0].GetProperty("sections").EnumerateArray())
        {
            string polilinha = seccao.GetProperty("polyline").GetString() ?? throw new FormatException("Secção sem linha.");

            foreach (Coordenadas ponto in PolilinhaFlexivel.Descodificar(polilinha))
            {
                // Cada secção começa onde a anterior acabou: não repetir esse ponto.
                if (linha.Count > 0 && linha[^1] == ponto)
                    continue;

                linha.Add(ponto);
            }
        }

        return linha.Count >= 2 ? linha : null;
    }

    /// <summary>Retira um ponto igual ao anterior (por exemplo, a recolha de um passageiro na origem da viagem).</summary>
    private static List<Coordenadas> SemRepetidosSeguidos(IReadOnlyList<Coordenadas> pontos)
    {
        var resultado = new List<Coordenadas>();

        foreach (Coordenadas ponto in pontos)
        {
            if (resultado.Count == 0 || resultado[^1] != ponto)
                resultado.Add(ponto);
        }

        return resultado;
    }

    /// <summary>
    /// Coordenadas no formato da HERE: «latitude,longitude», com ponto decimal e até 6 casas.
    /// O SQL Server devolve decimal(9,6) com zeros à direita (41.230000); o formato fixo dá o mesmo texto em qualquer base de dados.
    /// </summary>
    private static string Texto(Coordenadas ponto) =>
        ponto.Latitude.ToString("0.######", CultureInfo.InvariantCulture) + "," + ponto.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
}
