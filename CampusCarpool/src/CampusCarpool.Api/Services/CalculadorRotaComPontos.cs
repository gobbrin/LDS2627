
namespace CampusCarpool.Api.Services;

public sealed class CalculadorRotaComPontos(
    MapasService mapas)
{
    public async Task<ResultadoRotaComPontos?> CalcularAsync(
        IReadOnlyList<PontoRota> pontosOrdenados,
        CancellationToken ct)
    {
        if (pontosOrdenados.Count < 2)
            throw new ArgumentException(
                "A rota precisa de pelo menos dois pontos.",
                nameof(pontosOrdenados));

        var segmentos = new List<SegmentoRota>();
        decimal distanciaTotal = 0;
        int duracaoTotal = 0;

        for (int i = 0; i < pontosOrdenados.Count - 1; i++)
        {
            var origem = pontosOrdenados[i];
            var destino = pontosOrdenados[i + 1];

            if (origem.Coordenadas == destino.Coordenadas)
            {
                segmentos.Add(new SegmentoRota(
                    origem,
                    destino,
                    0m,
                    0));

                continue;
            }


            var resultado = await mapas.CalcularAsync(
                origem.Coordenadas,
                destino.Coordenadas,
                ct);

            if (resultado is null)
                return null;

            segmentos.Add(new SegmentoRota(
                origem,
                destino,
                resultado.DistanciaKm,
                resultado.DuracaoMinutos));

            distanciaTotal += resultado.DistanciaKm;
            duracaoTotal += resultado.DuracaoMinutos;
        }

        return new ResultadoRotaComPontos(
            pontosOrdenados,
            segmentos,
            distanciaTotal,
            duracaoTotal);
    }
}
