using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Rota entre dois pontos: distância em km (2 casas) e duração em minutos (arredondada para cima).</summary>
/// <param name="DistanciaKm">Distância, em km.</param>
/// <param name="DuracaoMinutos">Duração, em minutos.</param>
/// <param name="Origem">Coordenadas da origem.</param>
/// <param name="Destino">Coordenadas do destino.</param>
/// <param name="Geometria">
/// Linha da rota para o mapa, quando o fornecedor a devolve no mesmo pedido (HERE Routing v8, D-5); <c>null</c> na
/// reserva (OpenRouteService), e aí quem chama pede a linha à parte ou usa uma reta.
/// </param>
public sealed record ResultadoRota(
    decimal DistanciaKm,
    int DuracaoMinutos,
    Coordenadas Origem,
    Coordenadas Destino,
    IReadOnlyList<Coordenadas>? Geometria = null);

/// <summary>Matriz de rotas: [i][j] é a duração (s) ou distância (m) do ponto i ao j; <c>null</c> se não houver rota.</summary>
public sealed record MatrizRotas(
    double?[][] DuracoesSegundos,
    double?[][] DistanciasMetros);

/// <summary>Contrato do Serviço de Mapas e Rotas (F008), sem acoplar as outras camadas a um fornecedor.</summary>
/// <remarks>
/// Implementações em <c>Infrastructure/Integrations</c> (D-5): <c>ServicoMapasHere</c> (principal),
/// <c>ServicoMapasRotas</c> (OpenRouteService, reserva) e <c>ServicoMapasComReserva</c>, que é a registada: tenta a
/// HERE e, se falhar, o OpenRouteService, e guarda as geocodificações em memória (D-3). Quem chama decide o que fazer
/// quando o serviço falha (P-19, RF-F008-12): por regra, um erro 502 se o serviço responder com erro e um 504 se o
/// tempo esgotar.
/// </remarks>
public interface IServicoMapasRotas
{
    /// <summary>Indica se o serviço está configurado; sem isso, os outros métodos lançam exceção.</summary>
    bool Configurado { get; }

    /// <summary>Converte um local em coordenadas; devolve <c>null</c> se não o encontrar.</summary>
    /// <exception cref="InvalidOperationException">Serviço por configurar.</exception>
    /// <exception cref="HttpRequestException">O serviço respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O serviço não respondeu a tempo.</exception>
    Task<Coordenadas?> GeocodificarAsync(string texto, CancellationToken ct);

    /// <summary>Calcula a rota direta entre dois pontos; devolve <c>null</c> se não houver rota.</summary>
    /// <exception cref="InvalidOperationException">Serviço por configurar.</exception>
    /// <exception cref="HttpRequestException">O serviço respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O serviço não respondeu a tempo.</exception>
    Task<ResultadoRota?> CalcularAsync(Coordenadas origem, Coordenadas destino, CancellationToken ct);

    /// <summary>Calcula as durações e as distâncias entre todos os pares de pontos, num só pedido.</summary>
    /// <exception cref="InvalidOperationException">Serviço por configurar.</exception>
    /// <exception cref="ArgumentException">Menos de dois pontos.</exception>
    /// <exception cref="HttpRequestException">O serviço respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O serviço não respondeu a tempo.</exception>
    Task<MatrizRotas?> CalcularMatrizAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct);
}
