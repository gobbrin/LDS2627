using System.Text.Json;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Services;

/// <summary>
/// Rota guardada de uma viagem, com os pontos das participações aceites (RF-F008-03 a RF-F008-05, D-025).
/// </summary>
/// <remarks>
/// A rota é calculada ao criar e alterar a viagem e ao aceitar ou cancelar uma participação, e fica em
/// <see cref="Viagem.RotaPontos"/>; a consulta lê-a sem chamar o Serviço de Mapas. <c>RotaPontos</c> a <c>null</c>
/// significa «por recalcular»: acontece quando um cancelamento não conseguiu recalcular (P-19), e a rota é
/// recalculada na consulta seguinte em que o serviço responda.
/// </remarks>
public sealed class RotaViagemService(MapasService mapas) : IRotaViagemService
{
    /// <summary>Calcula a rota da viagem com os pontos das participações indicadas, sem gravar nada.</summary>
    /// <param name="viagem">Viagem.</param>
    /// <param name="participacoes">Participações a incluir (a chamada decide quais: aceites, ou aceites mais um pedido).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se o serviço não encontrar nenhuma.</returns>
    /// <exception cref="HttpRequestException">O Serviço de Mapas respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O Serviço de Mapas não respondeu a tempo.</exception>
    public Task<ResultadoRotaComPontos?> CalcularAsync(
        Viagem viagem,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(participacoes);

        return OtimizadorRotas.CalcularAsync(
            mapas,
            Origem(viagem),
            Destino(viagem),
            participacoes.SelectMany(PontosDe).ToList(),
            ct);
    }

    /// <inheritdoc />
    public async Task<bool> RecalcularAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        if (!mapas.Configurado) return false;

        try
        {
            var rota = await CalcularAsync(viagem, aceites, ct);
            if (rota is null) return false;

            Guardar(viagem, rota);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            // Resposta sem o formato esperado: conta como serviço indisponível, para não falhar um cancelamento.
            return false;
        }
    }

    /// <summary>Devolve a rota guardada; se estiver por recalcular, calcula-a e grava-a (P-19).</summary>
    /// <param name="db">Contexto em que a viagem está a ser seguida.</param>
    /// <param name="viagem">Viagem, seguida pelo contexto.</param>
    /// <param name="aceites">Participações aceites da viagem.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se o serviço não encontrar nenhuma.</returns>
    /// <exception cref="HttpRequestException">O Serviço de Mapas respondeu com erro.</exception>
    /// <exception cref="TaskCanceledException">O Serviço de Mapas não respondeu a tempo.</exception>
    public async Task<ResultadoRotaComPontos?> ObterAsync(
        CampusCarpoolDbContext db,
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        var guardada = Ler(viagem);
        if (guardada is not null) return guardada;

        var rota = await CalcularAsync(viagem, aceites, ct);
        if (rota is null) return null;

        Guardar(viagem, rota);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Outra operação mudou a viagem entretanto: esta consulta responde na mesma e a seguinte recalcula.
            db.Entry(viagem).State = EntityState.Detached;
        }

        return rota;
    }

    /// <summary>Lê a rota guardada; <c>null</c> se estiver por recalcular.</summary>
    public static ResultadoRotaComPontos? Ler(Viagem viagem) =>
        viagem.RotaPontos is null ? null : RotaGuardada.Ler(viagem.RotaPontos);

    /// <summary>Escreve a rota na viagem, sem gravar. A distância acompanha a rota atual; a duração de P-04 não.</summary>
    public static void Guardar(Viagem viagem, ResultadoRotaComPontos rota)
    {
        viagem.RotaPontos = RotaGuardada.Serializar(rota);
        viagem.DistanciaKm = rota.DistanciaTotalKm;
    }

    /// <summary>Escreve na viagem a rota direta devolvida pelo Serviço de Mapas (criação, ainda sem passageiros).</summary>
    public static void GuardarDireta(Viagem viagem, ResultadoRota direta)
    {
        var origem = Origem(viagem);
        var destino = Destino(viagem);

        Guardar(viagem, new ResultadoRotaComPontos(
            [origem, destino],
            [new SegmentoRota(origem, destino, direta.DistanciaKm, direta.DuracaoMinutos)],
            direta.DistanciaKm,
            direta.DuracaoMinutos));
    }

    private static PontoRota Origem(Viagem viagem) => new(
        null,
        TiposPontoRota.Origem,
        viagem.Origem,
        new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude));

    private static PontoRota Destino(Viagem viagem) => new(
        null,
        TiposPontoRota.DestinoViagem,
        viagem.Destino,
        new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude));

    // Um ponto omitido segue a origem ou o destino da viagem (P-20), por isso não acrescenta paragens.
    private static IEnumerable<PontoRota> PontosDe(Participacao participacao)
    {
        if (!string.IsNullOrWhiteSpace(participacao.Recolha))
        {
            yield return new PontoRota(
                participacao.Id,
                TiposPontoRota.Recolha,
                participacao.Recolha,
                CoordenadasDe(participacao.RecolhaLatitude, participacao.RecolhaLongitude, participacao.Id));
        }

        if (!string.IsNullOrWhiteSpace(participacao.Destino))
        {
            yield return new PontoRota(
                participacao.Id,
                TiposPontoRota.DestinoPassageiro,
                participacao.Destino,
                CoordenadasDe(participacao.DestinoLatitude, participacao.DestinoLongitude, participacao.Id));
        }
    }

    // O pedido guarda sempre as coordenadas do ponto indicado (A8); faltarem é um erro de dados.
    private static Coordenadas CoordenadasDe(decimal? latitude, decimal? longitude, Guid participacaoId) =>
        latitude is decimal lat && longitude is decimal lon
            ? new Coordenadas(lat, lon)
            : throw new InvalidOperationException($"Participação {participacaoId} sem coordenadas.");
}
