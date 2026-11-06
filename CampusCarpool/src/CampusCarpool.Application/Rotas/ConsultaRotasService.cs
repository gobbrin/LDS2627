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
    private const string TempoExcedido = "Tempo de resposta dos mapas excedido.";

    private static readonly Erro ViagemNaoEncontrada = new(TipoErro.NaoEncontrado, "Viagem não encontrada.");

    /// <inheritdoc />
    public async Task<Resultado<Coordenadas>> GeocodificarAsync(string texto, CancellationToken ct)
    {
        if (!mapas.Configurado) return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<Coordenadas>(
            async () =>
            {
                var resultado = await mapas.GeocodificarAsync(texto, ct);
                return resultado is null ? new Erro(TipoErro.NaoEncontrado) : resultado;
            },
            FalhaServicoExterno,
            TempoExcedido,
            ct);
    }

    /// <inheritdoc />
    public async Task<Resultado<ResultadoRota>> CalcularAsync(string origem, string destino, CancellationToken ct)
    {
        if (!mapas.Configurado) return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<ResultadoRota>(
            async () =>
            {
                var pontoOrigem = await mapas.GeocodificarAsync(origem, ct);
                var pontoDestino = await mapas.GeocodificarAsync(destino, ct);

                if (pontoOrigem is null || pontoDestino is null)
                    return new Erro(TipoErro.NaoProcessavel, "Localização não encontrada.");

                var rota = await mapas.CalcularAsync(pontoOrigem, pontoDestino, ct);
                return rota is null ? new Erro(TipoErro.NaoProcessavel, "Rota indisponível.") : rota;
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
            async () =>
            {
                var rota = await rotas.ObterAsync(viagem, aceites, ct);

                if (rota is null)
                    return new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

                if (rota.DistanciaTotalKm <= 0)
                    return new Erro(TipoErro.NaoProcessavel, "Não existem dados suficientes para estimar o custo.");

                var visivel = veOsPontos ? rota : SemPontosDasParticipacoes(rota);

                return new RotaDaViagem(
                    viagem.Id,
                    aceites.Count,
                    visivel.PontosOrdenados,
                    visivel.Segmentos,
                    rota.DistanciaTotalKm,
                    rota.DuracaoTotalMinutos,
                    custos.Calcular(rota.DistanciaTotalKm, aceites.Count));
            },
            "Serviço de mapas indisponível.",
            "Tempo de resposta do serviço de mapas excedido.",
            ct);
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
