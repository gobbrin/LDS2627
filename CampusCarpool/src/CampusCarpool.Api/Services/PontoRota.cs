using System.Text.Json;

namespace CampusCarpool.Api.Services;

/// <summary>Tipos de ponto de uma rota.</summary>
public static class TiposPontoRota
{
    /// <summary>Origem da viagem.</summary>
    public const string Origem = "Origem";
    /// <summary>Destino da viagem.</summary>
    public const string DestinoViagem = "DestinoViagem";
    /// <summary>Recolha de um passageiro.</summary>
    public const string Recolha = "Recolha";
    /// <summary>Destino de um passageiro.</summary>
    public const string DestinoPassageiro = "DestinoPassageiro";
}

/// <summary>Ponto de uma rota.</summary>
/// <param name="ParticipacaoId">Participação a que o ponto pertence; <c>null</c> na origem e no destino da viagem.</param>
/// <param name="Tipo">Um dos valores de <see cref="TiposPontoRota"/>.</param>
/// <param name="Local">Texto do local, tal como foi indicado.</param>
/// <param name="Coordenadas">Coordenadas geocodificadas.</param>
public sealed record PontoRota(
    Guid? ParticipacaoId,
    string Tipo,
    string Local,
    Coordenadas Coordenadas
);

/// <summary>Troço entre dois pontos consecutivos, com distância (km) e duração (min).</summary>
public sealed record SegmentoRota(
    PontoRota Origem,
    PontoRota Destino,
    decimal DistanciaKm,
    int DuracaoMinutos
);

/// <summary>Rota completa: pontos por ordem, segmentos e totais (soma dos segmentos).</summary>
public sealed record ResultadoRotaComPontos(
    IReadOnlyList<PontoRota> PontosOrdenados,
    IReadOnlyList<SegmentoRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos
);

/// <summary>Formato JSON da coluna <c>Viagem.RotaPontos</c> (modelo de dados: <c>rota_pontos</c>).</summary>
/// <remarks>
/// Cada elemento tem a ordem, o tipo, o local, as coordenadas, se é um ponto adicional e, para os passageiros, a
/// participação; acrescenta a distância e a duração desde o ponto anterior, para a consulta não chamar o Serviço de
/// Mapas. Um ponto é adicional quando não coincide com a origem nem com o destino da viagem (RF-F003-03).
/// </remarks>
public static class RotaGuardada
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    private sealed record PontoGuardado(
        int Ordem,
        string Tipo,
        Guid? ParticipacaoId,
        string Local,
        decimal Latitude,
        decimal Longitude,
        bool Adicional,
        decimal DistanciaKm,
        int DuracaoMinutos);

    /// <summary>Converte a rota para o texto JSON guardado na viagem.</summary>
    public static string Serializar(ResultadoRotaComPontos rota)
    {
        var inicio = rota.PontosOrdenados[0].Coordenadas;
        var fim = rota.PontosOrdenados[^1].Coordenadas;

        var pontos = rota.PontosOrdenados.Select((ponto, i) => new PontoGuardado(
            i,
            ponto.Tipo,
            ponto.ParticipacaoId,
            ponto.Local,
            ponto.Coordenadas.Latitude,
            ponto.Coordenadas.Longitude,
            ponto.ParticipacaoId is not null && ponto.Coordenadas != inicio && ponto.Coordenadas != fim,
            i == 0 ? 0m : rota.Segmentos[i - 1].DistanciaKm,
            i == 0 ? 0 : rota.Segmentos[i - 1].DuracaoMinutos));

        return JsonSerializer.Serialize(pontos, Opcoes);
    }

    /// <summary>Lê o texto JSON guardado; devolve a rota com os segmentos e os totais.</summary>
    public static ResultadoRotaComPontos Ler(string json)
    {
        var guardados = JsonSerializer.Deserialize<List<PontoGuardado>>(json, Opcoes)
            ?? throw new InvalidOperationException("Rota guardada inválida.");

        var pontos = guardados
            .Select(p => new PontoRota(
                p.ParticipacaoId,
                p.Tipo,
                p.Local,
                new Coordenadas(p.Latitude, p.Longitude)))
            .ToList();

        var segmentos = guardados
            .Skip(1)
            .Select((p, i) => new SegmentoRota(pontos[i], pontos[i + 1], p.DistanciaKm, p.DuracaoMinutos))
            .ToList();

        return new ResultadoRotaComPontos(
            pontos,
            segmentos,
            segmentos.Sum(s => s.DistanciaKm),
            segmentos.Sum(s => s.DuracaoMinutos));
    }

    /// <summary>Número de pontos adicionais da rota (RF-F003-03); 0 com a rota por recalcular.</summary>
    public static int ContarAdicionais(string? json) => json is null
        ? 0
        : (JsonSerializer.Deserialize<List<PontoGuardado>>(json, Opcoes) ?? []).Count(p => p.Adicional);
}
