using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Rotas;

/// <summary>Implementação de <see cref="IConsultaRotasService"/>.</summary>
public sealed class ConsultaRotasService(
    IServicoMapasRotas mapas,
    IRotaService rotas,
    EstimativaCustoService custos,
    IConsultaViagensService consultas,
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes) : IConsultaRotasService
{
    private const string FalhaServicoExterno = "Falha no serviço externo de mapas.";
    private const string TempoExcedido = "Tempo de resposta do serviço de mapas excedido.";

    private static readonly Erro ViagemNaoEncontrada = new(TipoErro.NaoEncontrado, "Viagem não encontrada.");

    /// <inheritdoc />
    public async Task<Resultado<CoordenadasResposta>> GeocodificarAsync(string texto, CancellationToken ct)
    {
        if (!mapas.Configurado) return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<CoordenadasResposta>(
            async limite =>
            {
                Coordenadas? resultado = await mapas.GeocodificarAsync(texto, limite);

                if (resultado is null)
                    return new Erro(TipoErro.NaoEncontrado, "Local não encontrado.");

                return new CoordenadasResposta(resultado.Latitude, resultado.Longitude);
            },
            FalhaServicoExterno,
            TempoExcedido,
            ct);
    }

    /// <inheritdoc />
    public async Task<Resultado<RotaCalculada>> CalcularAsync(string origem, string destino, CancellationToken ct)
    {
        if (!mapas.Configurado) return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<RotaCalculada>(
            async limite =>
            {
                var pontoOrigem = await mapas.GeocodificarAsync(origem, limite);
                var pontoDestino = await mapas.GeocodificarAsync(destino, limite);

                if (pontoOrigem is null || pontoDestino is null)
                    return new Erro(TipoErro.NaoProcessavel, "Localização não encontrada.");

                var rota = await mapas.CalcularAsync(pontoOrigem, pontoDestino, limite);
                if (rota is null)
                    return new Erro(TipoErro.NaoProcessavel, "Rota indisponível.");

                // DEF-BE-054: a linha vem da HERE; sem ela, uma reta da origem ao destino. Usa o ct do pedido e não o
                // limite: a HERE tem o seu próprio limite de 10 s e uma falha dela nunca faz falhar a operação.
                IReadOnlyList<Coordenadas>? linha = await rotas.PedirLinhaAsync([pontoOrigem, pontoDestino], ct);
                bool aproximada = linha is null;
                linha ??= [pontoOrigem, pontoDestino];

                return new RotaCalculada(
                    rota.DistanciaKm,
                    rota.DuracaoMinutos,
                    new CoordenadasResposta(rota.Origem.Latitude, rota.Origem.Longitude),
                    new CoordenadasResposta(rota.Destino.Latitude, rota.Destino.Longitude),
                    GeometriaRota.EmPares(linha),
                    aproximada);
            },
            FalhaServicoExterno,
            TempoExcedido,
            ct);
    }

    /// <inheritdoc />
    public async Task<Resultado<RotaDaViagem>> ConsultarDaViagemAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        // Seguida pelo contexto: uma rota por recalcular é gravada ao ser obtida.
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ViagemNaoEncontrada;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        if (!await consultas.PodeConsultarAsync(viagem, utilizadorId, ct)) return ViagemNaoEncontrada;

        var veOsPontos = PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceites.Any(p => p.PassageiroId == utilizadorId));

        if (viagem.RotaPontos is null && !mapas.Configurado) return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<RotaDaViagem>(
            async limite =>
            {
                var rota = await rotas.ObterAsync(viagem, aceites, limite);

                if (rota is null)
                    return new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

                if (rota.DistanciaTotalKm <= 0)
                    return new Erro(TipoErro.NaoProcessavel, "Não existem dados suficientes para estimar o custo.");

                var visivel = veOsPontos ? rota : SemPontosDasParticipacoes(rota);
                IReadOnlyList<Coordenadas>? linha = LinhaVisivel(rota, veOsPontos);

                return new RotaDaViagem(
                    viagem.Id,
                    aceites.Count,
                    PontosParaResposta(visivel),
                    TrocosParaResposta(visivel),
                    rota.DistanciaTotalKm,
                    rota.DuracaoTotalMinutos,
                    custos.Calcular(rota.DistanciaTotalKm, aceites.Count),
                    GeometriaRota.EmPares(linha ?? GeometriaRota.LinhaReta(visivel.PontosOrdenados)),
                    linha is null);
            },
            "Serviço de mapas indisponível.",
            "Tempo de resposta do serviço de mapas excedido.",
            ct);
    }

    /// <summary>Converte os pontos do modelo interno nos pontos da resposta, com a ordem e sem a participação (BE-06).</summary>
    /// <remarks>Também usado pela simulação da alteração da viagem (<c>ViagemService.SimularAlteracaoAsync</c>).</remarks>
    internal static List<PontoDaRota> PontosParaResposta(ResultadoRotaComPontos rota)
    {
        var pontos = new List<PontoDaRota>();

        for (int ordem = 0; ordem < rota.PontosOrdenados.Count; ordem++)
        {
            PontoRota ponto = rota.PontosOrdenados[ordem];
            pontos.Add(new PontoDaRota(ordem, ponto.Tipo, ponto.Local, ponto.Coordenadas.Latitude, ponto.Coordenadas.Longitude));
        }

        return pontos;
    }

    /// <summary>Converte os segmentos em troços: o troço i vai do ponto de ordem i ao de ordem i + 1.</summary>
    internal static List<TrocoDaRota> TrocosParaResposta(ResultadoRotaComPontos rota)
    {
        var trocos = new List<TrocoDaRota>();

        for (int i = 0; i < rota.Segmentos.Count; i++)
        {
            SegmentoRota segmento = rota.Segmentos[i];
            trocos.Add(new TrocoDaRota(i, i + 1, segmento.DistanciaKm, segmento.DuracaoMinutos));
        }

        return trocos;
    }

    /// <summary>Linha da HERE que o utilizador pode ver; <c>null</c> se tiver de ver retas entre os pontos.</summary>
    /// <remarks>
    /// P-13: quem não participa na viagem não vê por onde a rota passa quando há paragens de passageiros, porque a
    /// linha mostraria as recolhas. Sem paragens (só origem e destino), a linha não revela nada e mostra-se.
    /// </remarks>
    private static IReadOnlyList<Coordenadas>? LinhaVisivel(ResultadoRotaComPontos rota, bool veOsPontos)
    {
        if (rota.Geometria is null)
            return null;

        if (veOsPontos || rota.PontosOrdenados.Count == 2)
            return rota.Geometria;

        return null;
    }

    // P-13: um só segmento da origem ao destino, com os totais, sem revelar por onde a rota passa.
    private static ResultadoRotaComPontos SemPontosDasParticipacoes(ResultadoRotaComPontos rota)
    {
        var origem = rota.PontosOrdenados[0];
        var destino = rota.PontosOrdenados[^1];

        return rota with
        {
            PontosOrdenados = [origem, destino],
            Segmentos = [new SegmentoRota(origem, destino, rota.DistanciaTotalKm, rota.DuracaoTotalMinutos)]
        };
    }
}
