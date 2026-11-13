using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Consultas ao Serviço de Mapas e às rotas das viagens expostas ao frontend (F008).</summary>
/// <remarks>
/// Todos os métodos respondem com erro 503 se o serviço não estiver configurado, 502 se falhar e 504 se exceder o
/// tempo (RF-F008-12). O cálculo e a gravação da rota estão no <see cref="IRotaService"/>.
/// </remarks>
public interface IConsultaRotasService
{
    /// <summary>Converte um texto num par de coordenadas.</summary>
    /// <returns>As coordenadas, ou erro 404 se o local não for encontrado.</returns>
    Task<Resultado<Coordenadas>> GeocodificarAsync(string texto, CancellationToken ct);

    /// <summary>Calcula a rota direta entre dois locais, sem gravar nada.</summary>
    /// <returns>A rota, ou erro 422 se um local ou a rota não forem encontrados.</returns>
    Task<Resultado<ResultadoRota>> CalcularAsync(string origem, string destino, CancellationToken ct);

    /// <summary>
    /// Rota de uma viagem com os pontos das participações aceites, na ordem calculada, e a estimativa de custo
    /// (RF-F008-03 a RF-F008-05, RF-F008-10).
    /// </summary>
    /// <remarks>
    /// O condutor e os passageiros aceites veem a rota completa (P-13, RF-F008-09). Quem pode consultar a viagem vê
    /// a rota entre a origem e o destino, sem os pontos das participações (RF-F003-08, DEF-BE-013). Lê a rota
    /// guardada, sem chamar o Serviço de Mapas; uma rota por recalcular é recalculada aqui (P-19) e, se o serviço
    /// falhar, responde que a rota não está disponível.
    /// </remarks>
    /// <returns>A rota; erro 404 se a viagem não existir ou o utilizador não a puder consultar; 422 se a rota não for calculável.</returns>
    Task<Resultado<RotaDaViagem>> ConsultarDaViagemAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}
