using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Rotas;
using Microsoft.Extensions.Configuration;

namespace CampusCarpool.Tests;

/// <summary>Serviço de push que não faz nada (para testes que não verificam o push).</summary>
public sealed class ServicoPushFalso : IServicoPush
{
    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Notificador em tempo real falso: regista os eventos para o teste os verificar.</summary>
public sealed class NotificadorPedidosFalso : INotificadorPedidosTempoReal
{
    public Task PedidoRecebidoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoAceiteAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoRejeitadoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Serviço de push falso que guarda cada envio, para verificar destinatários e conteúdo.</summary>
public sealed class ServicoPushRegistador : IServicoPush
{
    public List<(Guid UtilizadorId, string Titulo,
        string Mensagem, string Tipo)> Envios
    { get; } = new();

    public bool SimularFalha { get; set; }

    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
    {
        if (SimularFalha)
            throw new HttpRequestException("Falha simulada no serviço push.");

        Envios.Add((utilizadorId, titulo, mensagem, tipo));

        return Task.CompletedTask;
    }
}

/// <summary>Recálculo de rota falso, para testes que não verificam a rota. Com <see cref="Disponivel"/> a falso simula o Serviço de Mapas em baixo.</summary>
public sealed class RotaViagemFalsa : IRotaViagemService
{
    public bool Disponivel { get; set; } = true;

    public Task<bool> RecalcularAsync(
        CampusCarpool.Domain.Entities.Viagem viagem,
        IReadOnlyCollection<CampusCarpool.Domain.Entities.Participacao> aceites,
        CancellationToken ct)
        => Task.FromResult(Disponivel);
}

/// <summary>
/// OpenRouteService simulado: geocodificação, direções e matriz, sem chamadas externas. Conta os pedidos por tipo.
/// </summary>
/// <remarks>
/// Cada texto dá sempre as mesmas coordenadas (por omissão, derivadas do texto; ou as de <see cref="Locais"/>).
/// A matriz devolve <see cref="MetrosPorSegmento"/> e <see cref="SegundosPorSegmento"/> entre pontos diferentes,
/// ou o que <see cref="Duracao"/> indicar para os índices (origem, destino) dos pontos do pedido.
/// </remarks>
public sealed class MapasFalsos : HttpMessageHandler
{
    public int ChamadasGeocodificacao { get; private set; }
    public int ChamadasRotas { get; private set; }
    public bool SimularFalha { get; set; }
    public double MetrosPorSegmento { get; set; } = 1000;
    public double SegundosPorSegmento { get; set; } = 120;
    public Func<int, int, double>? Duracao { get; set; }
    public Dictionary<string, (double Latitude, double Longitude)> Locais { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cria o cliente de mapas que usa este simulador.</summary>
    public CampusCarpool.Infrastructure.Integrations.ServicoMapasRotas CriarServico(bool configurado = true)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = configurado ? "chave-ficticia-apenas-para-testes" : ""
            })
            .Build();

        return new CampusCarpool.Infrastructure.Integrations.ServicoMapasRotas(new HttpClient(this, disposeHandler: false), configuracao);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (SimularFalha)
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);

        var caminho = request.RequestUri!.AbsolutePath;

        if (caminho.Contains("/pelias/v1/search", StringComparison.OrdinalIgnoreCase))
        {
            ChamadasGeocodificacao++;
            var query = Uri.UnescapeDataString(request.RequestUri.Query);
            var inicio = query.IndexOf("text=", StringComparison.Ordinal) + 5;
            var (latitude, longitude) = Coordenadas(query[inicio..].Split('&')[0].Replace('+', ' '));
            return Json(new { features = new[] { new { geometry = new { coordinates = new[] { longitude, latitude } } } } });
        }

        ChamadasRotas++;

        if (caminho.Contains("matrix", StringComparison.OrdinalIgnoreCase))
        {
            using var corpo = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var n = corpo.RootElement.GetProperty("locations").GetArrayLength();
            var duracoes = Enumerable.Range(0, n)
                .Select(i => Enumerable.Range(0, n).Select(j => i == j ? 0 : Duracao?.Invoke(i, j) ?? SegundosPorSegmento).ToArray())
                .ToArray();
            var distancias = Enumerable.Range(0, n)
                .Select(i => Enumerable.Range(0, n).Select(j => i == j ? 0 : MetrosPorSegmento).ToArray())
                .ToArray();
            return Json(new { durations = duracoes, distances = distancias });
        }

        return Json(new
        {
            features = new[]
            {
                new { properties = new { summary = new { distance = MetrosPorSegmento, duration = SegundosPorSegmento } } }
            }
        });
    }

    private (double Latitude, double Longitude) Coordenadas(string texto)
    {
        if (Locais.TryGetValue(texto, out var conhecido)) return conhecido;

        var soma = texto.Aggregate(0, (acumulado, c) => (acumulado * 31 + c) % 10007);
        return (41 + soma / 100000d, -8 - soma / 100000d);
    }

    private static HttpResponseMessage Json(object valor) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(valor),
            System.Text.Encoding.UTF8,
            "application/json")
    };
}
