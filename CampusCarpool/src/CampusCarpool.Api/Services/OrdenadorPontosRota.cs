
namespace CampusCarpool.Api.Services;

public static class OrdenadorPontosRota
{
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
