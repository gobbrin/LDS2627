using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Formato JSON da coluna <c>Viagem.RotaPontos</c> (modelo de dados: <c>rota_pontos</c>).</summary>
/// <remarks>
/// <para>Desde a I112, a coluna guarda um objeto <c>{ "pontos": [...], "geometria": [[lat, lon], ...] }</c>. Cada
/// ponto tem a ordem, o tipo, o local, as coordenadas, se é um ponto adicional e, para os passageiros, a participação;
/// acrescenta a distância e a duração desde o ponto anterior, para a consulta não chamar o Serviço de Mapas. Um ponto
/// é adicional quando não coincide com a origem nem com o destino da viagem (RF-F003-03). A <c>geometria</c> é a linha
/// da HERE (DEF-BE-054), ou <c>null</c> se não houver.</para>
/// <para>Antes da I112 a coluna guardava só a lista de pontos; esse formato continua a ler-se, sem linha.</para>
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

    /// <summary>Objeto guardado na coluna: os pontos e a linha do mapa.</summary>
    private sealed record RotaEmJson(List<PontoGuardado> Pontos, List<decimal[]>? Geometria);

    /// <summary>Lê a rota guardada na viagem; <c>null</c> se estiver por recalcular.</summary>
    public static ResultadoRotaComPontos? Ler(Viagem viagem)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return viagem.RotaPontos is null ? null : Ler(viagem.RotaPontos);
    }

    /// <summary>Escreve a rota na viagem, sem gravar. A distância acompanha a rota atual; a duração de P-04 não.</summary>
    public static void Guardar(Viagem viagem, ResultadoRotaComPontos rota)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(rota);

        viagem.AtualizarRota(Serializar(rota), rota.DistanciaTotalKm);
    }

    /// <summary>
    /// Monta a rota direta (origem e destino, ainda sem passageiros) a partir do resultado do Serviço de Mapas.
    /// </summary>
    /// <param name="viagem">Viagem, já com a origem e o destino.</param>
    /// <param name="direta">Distância e duração da rota direta.</param>
    /// <returns>A rota com dois pontos e um segmento, sem linha (a linha junta-se ao guardar).</returns>
    public static ResultadoRotaComPontos Direta(Viagem viagem, ResultadoRota direta)
    {
        ArgumentNullException.ThrowIfNull(viagem);
        ArgumentNullException.ThrowIfNull(direta);

        var origem = PontoRota.OrigemDe(viagem);
        var destino = PontoRota.DestinoDe(viagem);

        return new ResultadoRotaComPontos(
            [origem, destino],
            [new SegmentoRota(origem, destino, direta.DistanciaKm, direta.DuracaoMinutos)],
            direta.DistanciaKm,
            direta.DuracaoMinutos);
    }

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
            i == 0 ? 0 : rota.Segmentos[i - 1].DuracaoMinutos)).ToList();

        List<decimal[]>? geometria = rota.Geometria is null ? null : GeometriaRota.EmPares(rota.Geometria);

        return JsonSerializer.Serialize(new RotaEmJson(pontos, geometria), Opcoes);
    }

    /// <summary>Lê o texto JSON guardado; devolve a rota com os segmentos, os totais e a linha (se houver).</summary>
    public static ResultadoRotaComPontos Ler(string json)
    {
        RotaEmJson guardada = LerJson(json);
        List<PontoGuardado> guardados = guardada.Pontos;

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

        List<Coordenadas>? geometria = guardada.Geometria is null ? null : GeometriaRota.DePares(guardada.Geometria);

        return new ResultadoRotaComPontos(
            pontos,
            segmentos,
            segmentos.Sum(s => s.DistanciaKm),
            segmentos.Sum(s => s.DuracaoMinutos),
            geometria);
    }

    /// <summary>Número de pontos adicionais da rota (RF-F003-03); 0 com a rota por recalcular.</summary>
    public static int ContarAdicionais(string? json) => json is null ? 0 : LerJson(json).Pontos.Count(p => p.Adicional);

    /// <summary>Lê o texto da coluna nos dois formatos: o antigo (só a lista de pontos) e o da I112 (objeto).</summary>
    /// <exception cref="InvalidOperationException">Texto que não é uma rota.</exception>
    private static RotaEmJson LerJson(string json)
    {
        // O formato antigo é uma lista: começa por «[». Não tem linha.
        if (json.TrimStart().StartsWith('['))
        {
            List<PontoGuardado> pontos = JsonSerializer.Deserialize<List<PontoGuardado>>(json, Opcoes)
                ?? throw new InvalidOperationException("Rota guardada inválida.");

            return new RotaEmJson(pontos, null);
        }

        RotaEmJson? rota = JsonSerializer.Deserialize<RotaEmJson>(json, Opcoes);

        if (rota?.Pontos is null)
            throw new InvalidOperationException("Rota guardada inválida.");

        return rota;
    }
}
