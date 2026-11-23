using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>
/// Fornecedor da linha da rota, para os frontends a desenharem no mapa (RF-F008-07, DEF-BE-054).
/// </summary>
/// <remarks>
/// A implementação usa o HERE Routing v8 (<c>Infrastructure/Integrations/ServicoGeometriaHere.cs</c>), com a chave em
/// <c>Here:ApiKey</c>. A distância, a duração e a ordem dos pontos continuam a vir do <see cref="IServicoMapasRotas"/>;
/// daqui vem só a linha. Quem chama usa linhas retas entre os pontos (a reserva) quando este serviço não está
/// configurado ou devolve <c>null</c>.
/// </remarks>
public interface IServicoGeometriaRotas
{
    /// <summary>Indica se há chave configurada; sem ela, não se faz nenhum pedido.</summary>
    bool Configurado { get; }

    /// <summary>Pede a linha da rota que passa pelos pontos indicados, pela ordem indicada.</summary>
    /// <param name="pontos">Origem, paragens e destino, já pela ordem da rota (pelo menos dois).</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <returns>
    /// A linha, como lista de coordenadas desde a origem até ao destino; <c>null</c> se o serviço não estiver
    /// configurado, falhar, não responder a tempo ou não encontrar rota. Nunca lança por falha do serviço.
    /// </returns>
    Task<IReadOnlyList<Coordenadas>?> CalcularLinhaAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct);
}
