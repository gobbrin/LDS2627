using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Rota entre dois pontos: distância em km (2 casas) e duração em minutos (arredondada para cima).</summary>
public sealed record ResultadoRota(
    decimal DistanciaKm,
    int DuracaoMinutos,
    Coordenadas Origem,
    Coordenadas Destino);

/// <summary>Matriz de rotas: [i][j] é a duração (s) ou distância (m) do ponto i ao j; <c>null</c> se não houver rota.</summary>
public sealed record MatrizRotas(
    double?[][] DuracoesSegundos,
    double?[][] DistanciasMetros);

/// <summary>Contrato do Serviço de Mapas e Rotas (F008), sem acoplar as outras camadas a um fornecedor.</summary>
/// <remarks>
/// A implementação (OpenRouteService) está em <c>Infrastructure/Integrations/ServicoMapasRotas.cs</c>. Quem chama
/// decide o que fazer quando o serviço falha (P-19, RF-F008-12): nos controllers, 502 para um erro do serviço e 504
/// para o tempo esgotado.
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
