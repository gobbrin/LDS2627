using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Rota guardada de uma viagem, com os pontos das participações aceites (RF-F008-03 a RF-F008-05, D-025).</summary>
public interface IRotaService
{
    /// <summary>Calcula a rota da viagem com os pontos das participações indicadas, sem gravar nada.</summary>
    /// <param name="viagem">Viagem.</param>
    /// <param name="participacoes">Participações a incluir (a chamada decide quais: aceites, ou aceites mais um pedido).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se o serviço não encontrar nenhuma.</returns>
    /// <exception cref="HttpRequestException">O Serviço de Mapas respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O Serviço de Mapas não respondeu a tempo.</exception>
    Task<ResultadoRotaComPontos?> CalcularAsync(
        Viagem viagem,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct);

    /// <summary>Calcula a rota entre os pontos indicados com os das participações, sem gravar nada.</summary>
    /// <remarks>Serve para a alteração de uma viagem, em que a origem e o destino ainda não estão na entidade.</remarks>
    /// <param name="origem">Origem da rota.</param>
    /// <param name="destino">Destino da rota.</param>
    /// <param name="participacoes">Participações a incluir.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se o serviço não encontrar nenhuma.</returns>
    /// <exception cref="HttpRequestException">O Serviço de Mapas respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O Serviço de Mapas não respondeu a tempo.</exception>
    Task<ResultadoRotaComPontos?> CalcularEntreAsync(
        PontoRota origem,
        PontoRota destino,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct);

    /// <summary>Recalcula a rota com as participações indicadas e escreve-a na viagem, sem gravar.</summary>
    /// <param name="viagem">Viagem a atualizar.</param>
    /// <param name="aceites">Participações que a rota deve incluir.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns><c>false</c>, sem alterar a viagem, se o Serviço de Mapas estiver indisponível (P-19).</returns>
    Task<bool> RecalcularAsync(Viagem viagem, IReadOnlyCollection<Participacao> aceites, CancellationToken ct);

    /// <summary>Devolve a rota guardada; se estiver por recalcular, calcula-a e grava-a (P-19).</summary>
    /// <param name="viagem">Viagem, seguida pelo contexto do pedido.</param>
    /// <param name="aceites">Participações aceites da viagem.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se o serviço não encontrar nenhuma.</returns>
    /// <exception cref="HttpRequestException">O Serviço de Mapas respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O Serviço de Mapas não respondeu a tempo.</exception>
    Task<ResultadoRotaComPontos?> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct);
}
