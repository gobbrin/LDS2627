
namespace CampusCarpool.Api.Services;

/// <summary>Ordena os pontos adicionais de uma viagem, respeitando a recolha antes do destino de cada passageiro (RF-F004-04).</summary>
/// <remarks>
/// Heurística gulosa do vizinho mais próximo: a partir do ponto atual escolhe o candidato mais perto em linha reta.
/// Não garante P-07 (menor duração total) e não é o algoritmo da simulação, o <c>OtimizadorRotas</c>, por isso a
/// rota mostrada depois da aceitação pode diferir da simulada (DEF-BE-022, fica para m4).
/// </remarks>
public static class OrdenadorPontosRota
{
    /// <summary>Devolve origem, pontos adicionais ordenados e destino.</summary>
    /// <param name="origem">Origem da viagem (fica sempre em primeiro).</param>
    /// <param name="adicionais">Pontos <c>Recolha</c> e <c>DestinoPassageiro</c> das participações aceites.</param>
    /// <param name="destino">Destino da viagem (fica sempre em último).</param>
    /// <exception cref="InvalidOperationException">Nenhum ponto pode ser escolhido (dados incoerentes).</exception>
    public static List<PontoRota> Ordenar(
        PontoRota origem,
        IEnumerable<PontoRota> adicionais,
        PontoRota destino)
    {
        var restantes = adicionais.ToList();
        var ordenados = new List<PontoRota> { origem };

        // Passageiros sem recolha específica entram na origem.
        var idsComRecolha = restantes
            .Where(p =>
                p.Tipo == "Recolha" &&
                p.ParticipacaoId.HasValue)
            .Select(p => p.ParticipacaoId!.Value)
            .ToHashSet();

        var recolhidos = restantes
            .Where(p =>
                p.Tipo == "DestinoPassageiro" &&
                p.ParticipacaoId is Guid id &&
                !idsComRecolha.Contains(id))
            .Select(p => p.ParticipacaoId!.Value)
            .ToHashSet();

        var atual = origem;

        while (restantes.Count > 0)
        {
            var candidatos = restantes
                .Where(p =>
                    p.Tipo != "DestinoPassageiro" ||
                    p.ParticipacaoId is Guid id &&
                    recolhidos.Contains(id))
                .ToList();

            if (candidatos.Count == 0)
                throw new InvalidOperationException(
                    "Não é possível ordenar os pontos.");

            var proximo = candidatos
                .OrderBy(p => DistanciaAproximada(
                    atual.Coordenadas,
                    p.Coordenadas))
                .First();

            ordenados.Add(proximo);
            restantes.Remove(proximo);

            if (proximo.Tipo == "Recolha" &&
                proximo.ParticipacaoId is Guid participacaoId)
            {
                recolhidos.Add(participacaoId);
            }

            atual = proximo;
        }

        ordenados.Add(destino);

        return ordenados;
    }

    // Quadrado da distância aproximada em km (projeção equiretangular). Só serve para comparar distâncias
    // próximas, por isso dispensa a raiz quadrada e a fórmula de Haversine.
    private static double DistanciaAproximada(
        Coordenadas a,
        Coordenadas b)
    {
        const double kmPorGrauLatitude = 111.32;

        var latitudeMedia = (double)(
            (a.Latitude + b.Latitude) / 2m);

        var diferencaLatitude =
            (double)(a.Latitude - b.Latitude)
            * kmPorGrauLatitude;

        var diferencaLongitude =
            (double)(a.Longitude - b.Longitude)
            * kmPorGrauLatitude
            * Math.Cos(latitudeMedia * Math.PI / 180);

        return diferencaLatitude * diferencaLatitude +
               diferencaLongitude * diferencaLongitude;
    }
}
