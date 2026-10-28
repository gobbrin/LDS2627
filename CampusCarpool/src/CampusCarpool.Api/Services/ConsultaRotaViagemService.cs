
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Api.Services;

public sealed class ConsultaRotaViagemService(MapasService mapas)
{
    public async Task<ResultadoRotaComPontos?> CalcularAsync(
        Viagem viagem,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(participacoes);

        // 1. Obter as coordenadas da viagem.
        // Reutilizar as coordenadas guardadas, quando existem.
        Coordenadas? coordenadasOrigem =
            viagem.OrigemLatitude is decimal latOrigem &&
            viagem.OrigemLongitude is decimal lonOrigem
                ? new Coordenadas(latOrigem, lonOrigem)
                : await mapas.GeocodificarAsync(viagem.Origem, ct);

        Coordenadas? coordenadasDestino =
            viagem.DestinoLatitude is decimal latDestino &&
            viagem.DestinoLongitude is decimal lonDestino
                ? new Coordenadas(latDestino, lonDestino)
                : await mapas.GeocodificarAsync(viagem.Destino, ct);

        if (coordenadasOrigem is null ||
            coordenadasDestino is null)
        {
            return null;
        }

        // 2. Criar os pontos principais da viagem.
        var origem = new PontoRota(
            null,
            "Origem",
            viagem.Origem,
            coordenadasOrigem);

        var destino = new PontoRota(
            null,
            "DestinoViagem",
            viagem.Destino,
            coordenadasDestino);

        var adicionais = new List<PontoRota>();

        // 3. Considerar apenas participações aceites.
        var aceites = participacoes
            .Where(p =>
                p.ViagemId == viagem.Id &&
                p.Estado == EstadoParticipacao.Aceite)
            .ToList();

        foreach (var participacao in aceites)
        {
            // Se a recolha não for indicada,
            // considera-se a origem da viagem.
            if (!string.IsNullOrWhiteSpace(participacao.Recolha))
            {
                var coordenadasRecolha =
                    await mapas.GeocodificarAsync(
                        participacao.Recolha,
                        ct);

                if (coordenadasRecolha is null)
                    return null;

                adicionais.Add(new PontoRota(
                    participacao.Id,
                    "Recolha",
                    participacao.Recolha,
                    coordenadasRecolha));
            }

            // Se o destino não for indicado,
            // considera-se o destino da viagem.
            if (!string.IsNullOrWhiteSpace(participacao.Destino))
            {
                var coordenadasSaida =
                    await mapas.GeocodificarAsync(
                        participacao.Destino,
                        ct);

                if (coordenadasSaida is null)
                    return null;

                adicionais.Add(new PontoRota(
                    participacao.Id,
                    "DestinoPassageiro",
                    participacao.Destino,
                    coordenadasSaida));
            }
        }

        // 4. Ordenar os pontos, respeitando
        // recolha antes do destino de cada passageiro.
        var pontosOrdenados = OrdenadorPontosRota.Ordenar(
            origem,
            adicionais,
            destino);

        // 5. Calcular todos os segmentos da rota.
        var calculador = new CalculadorRotaComPontos(mapas);

        return await calculador.CalcularAsync(
            pontosOrdenados,
            ct);
    }
}
