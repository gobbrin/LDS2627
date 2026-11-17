using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>
/// Converte o modelo interno da rota (<see cref="ResultadoRotaComPontos"/>) nas respostas da API (BE-06, BE-41).
/// </summary>
/// <remarks>
/// Usado pela consulta da rota (<c>ConsultaRotasService</c>), pela simulação da alteração da viagem
/// (<c>ViagemService</c>) e pela simulação do pedido (<c>SimulacaoDePedidoService</c>): as três respostas têm o mesmo
/// formato, para os frontends usarem o mesmo mapa.
/// </remarks>
public static class RespostasDaRota
{
    /// <summary>Converte os pontos do modelo interno nos pontos da resposta, com a ordem e sem a participação (BE-06).</summary>
    /// <param name="rota">Rota calculada ou guardada.</param>
    /// <returns>Um <see cref="PontoDaRota"/> por ponto, pela ordem da rota (a origem tem a ordem 0).</returns>
    public static List<PontoDaRota> Pontos(ResultadoRotaComPontos rota)
    {
        ArgumentNullException.ThrowIfNull(rota);

        var pontos = new List<PontoDaRota>();

        for (int ordem = 0; ordem < rota.PontosOrdenados.Count; ordem++)
        {
            PontoRota ponto = rota.PontosOrdenados[ordem];
            pontos.Add(new PontoDaRota(ordem, ponto.Tipo, ponto.Local, ponto.Coordenadas.Latitude, ponto.Coordenadas.Longitude));
        }

        return pontos;
    }

    /// <summary>Converte os segmentos em troços: o troço i vai do ponto de ordem i ao de ordem i + 1.</summary>
    /// <param name="rota">Rota calculada ou guardada.</param>
    /// <returns>Um <see cref="TrocoDaRota"/> por segmento, com a distância e a duração.</returns>
    public static List<TrocoDaRota> Trocos(ResultadoRotaComPontos rota)
    {
        ArgumentNullException.ThrowIfNull(rota);

        var trocos = new List<TrocoDaRota>();

        for (int i = 0; i < rota.Segmentos.Count; i++)
        {
            SegmentoRota segmento = rota.Segmentos[i];
            trocos.Add(new TrocoDaRota(i, i + 1, segmento.DistanciaKm, segmento.DuracaoMinutos));
        }

        return trocos;
    }

    /// <summary>
    /// Monta a resposta completa de uma rota que o condutor vê toda (as simulações): pontos, troços, totais, custo e
    /// linha do mapa (RF-F008-03, 04, 07, 10).
    /// </summary>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="participacoesAceites">Passageiros aceites que a rota inclui (entram no custo por participante).</param>
    /// <param name="rota">Rota (simulada ou guardada).</param>
    /// <param name="custo">Estimativa de custo já calculada para esta rota.</param>
    /// <returns>
    /// A rota no formato de <c>GET /rotas/viagens/{id}</c>. Sem linha da HERE, a <c>geometria</c> são retas entre os
    /// pontos e <c>geometriaAproximada</c> é <c>true</c>.
    /// </returns>
    public static RotaDaViagem RotaCompleta(
        Guid viagemId,
        int participacoesAceites,
        ResultadoRotaComPontos rota,
        ResultadoEstimativaCusto custo)
    {
        ArgumentNullException.ThrowIfNull(rota);

        IReadOnlyList<Coordenadas> linha = rota.Geometria ?? GeometriaRota.LinhaReta(rota.PontosOrdenados);

        return new RotaDaViagem(
            viagemId,
            participacoesAceites,
            Pontos(rota),
            Trocos(rota),
            rota.DistanciaTotalKm,
            rota.DuracaoTotalMinutos,
            custo,
            GeometriaRota.EmPares(linha),
            rota.Geometria is null);
    }
}
