using CampusCarpool.Application.Comum;
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
    /// <remarks>
    /// O cálculo (matriz) corre com o limite de 10 s dos mapas; a linha da HERE pede-se depois, fora desse limite, e uma
    /// falha dela só deixa a rota sem linha (BE-36). Numa viagem que já não está ativa, uma rota por recalcular não é
    /// recalculada (BE-38 d).
    /// </remarks>
    /// <param name="viagem">Viagem, seguida pelo contexto do pedido.</param>
    /// <param name="aceites">Participações aceites da viagem.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <returns>
    /// A rota; ou o erro: 422 viagem que já não está ativa ou sem rota possível, 502, 503 (mapas por configurar) ou 504.
    /// </returns>
    Task<Resultado<ResultadoRotaComPontos>> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct);

    /// <summary>Junta a linha do mapa à rota e escreve-a na viagem, sem gravar (RF-F008-07, DEF-BE-054).</summary>
    /// <remarks>A linha vem de <see cref="JuntarLinhaAsync"/>. Nunca falha por causa da HERE.</remarks>
    /// <param name="viagem">Viagem a atualizar (ainda com a rota anterior, para a comparação).</param>
    /// <param name="rota">Rota calculada.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota tal como ficou guardada, com a linha (ou sem ela).</returns>
    Task<ResultadoRotaComPontos> GuardarAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct);

    /// <summary>Junta a linha do mapa à rota, sem a escrever na viagem.</summary>
    /// <remarks>
    /// Se a rota já tem linha (de uma simulação recente ou da rota direta da HERE), fica essa. Se a rota guardada na
    /// viagem passa pelos mesmos pontos e tem linha, reutiliza-a. Só nos outros casos pede a linha à HERE. Sem HERE, ou
    /// se ela falhar, a rota fica sem linha e o mapa mostra retas entre os pontos.
    /// </remarks>
    /// <param name="viagem">Viagem (com a rota guardada, para a comparação).</param>
    /// <param name="rota">Rota calculada.</param>
    /// <param name="ct">Cancelamento do pedido (não o limite de 10 s dos mapas).</param>
    /// <returns>A rota com a linha, ou sem ela.</returns>
    Task<ResultadoRotaComPontos> JuntarLinhaAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct);

    /// <summary>
    /// Termina uma simulação (do pedido ou da alteração da viagem, D-2): junta a linha da HERE e guarda a rota em
    /// memória durante 10 minutos, para a gravação seguinte com os mesmos pontos a reutilizar sem chamar os mapas.
    /// </summary>
    /// <remarks>Nada vai para a base de dados: uma simulação fica obsoleta assim que a viagem muda.</remarks>
    /// <param name="viagem">Viagem simulada.</param>
    /// <param name="rota">Rota simulada.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <returns>A rota simulada, com a linha (ou sem ela).</returns>
    Task<ResultadoRotaComPontos> ConcluirSimulacaoAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct);

    /// <summary>Pede à HERE a linha que passa pelos pontos indicados, pela ordem indicada.</summary>
    /// <param name="pontos">Origem, paragens e destino.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A linha; <c>null</c> se a HERE não estiver configurada ou não a der (o mapa usa retas).</returns>
    Task<IReadOnlyList<Coordenadas>?> PedirLinhaAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct);
}
