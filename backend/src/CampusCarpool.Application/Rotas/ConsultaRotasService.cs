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

                // DEF-BE-054 / D-5: a rota direta da HERE já traz a linha. Se a rota veio da reserva (OpenRouteService),
                // pede-se a linha à HERE; sem ela, uma reta da origem ao destino. Usa o ct do pedido e não o limite: a
                // HERE tem o seu próprio limite de 10 s e uma falha dela nunca faz falhar a operação.
                IReadOnlyList<Coordenadas>? linha = rota.Geometria ?? await rotas.PedirLinhaAsync([pontoOrigem, pontoDestino], ct);
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

        // D-1 (P-13): o condutor e os passageiros aceites veem os pontos de todos os aceites e a linha da HERE.
        var veOsPontos = PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceites.Any(p => p.PassageiroId == utilizadorId));

        // Rota guardada: sem chamadas. Por recalcular: a matriz corre com o limite de 10 s e a linha fora dele (BE-36).
        Resultado<ResultadoRotaComPontos> obtida = await rotas.ObterAsync(viagem, aceites, ct);

        if (obtida.Erro is { } erro)
            return erro;

        ResultadoRotaComPontos rota = obtida.Valor;

        if (rota.DistanciaTotalKm <= 0)
            return new Erro(TipoErro.NaoProcessavel, "Não existem dados suficientes para estimar o custo.");

        var visivel = veOsPontos ? rota : SemPontosDasParticipacoes(rota);
        // SP-01: a linha guardada é uma só e é igual para todos (percurso real da HERE). O que muda com quem vê são
        // os pontos (P-13): quem não participa não recebe as paragens dos passageiros nem os troços entre elas.
        IReadOnlyList<Coordenadas>? linha = rota.Geometria;

        return new RotaDaViagem(
            viagem.Id,
            aceites.Count,
            RespostasDaRota.Pontos(visivel),
            RespostasDaRota.Trocos(visivel),
            rota.DistanciaTotalKm,
            rota.DuracaoTotalMinutos,
            custos.Calcular(rota.DistanciaTotalKm, aceites.Count),
            GeometriaRota.EmPares(linha ?? GeometriaRota.LinhaReta(visivel.PontosOrdenados)),
            linha is null);
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
