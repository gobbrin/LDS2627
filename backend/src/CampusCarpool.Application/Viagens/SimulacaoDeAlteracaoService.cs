using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Viagens;

/// <summary>
/// Simulação da alteração da origem ou do destino de uma viagem (<c>POST /viagens/{id}/simular-alteracao</c>;
/// RF-F002-07, RF-F008-05, RF-F008-10).
/// </summary>
/// <remarks>
/// Saiu do <c>ViagemService</c> na fase R (AE-01): é um dos pontos que gastam a API de mapas, e assim fica numa classe
/// pequena, só com ele. As regras e o cálculo da rota são os do <c>PUT</c> (<see cref="AlteracaoDeViagem"/>).
/// </remarks>
public sealed class SimulacaoDeAlteracaoService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IServicoMapasRotas mapas,
    IRotaService rotas,
    EstimativaCustoService custos) : ISimulacaoDeAlteracaoService
{
    // As regras e o cálculo da rota do PUT /viagens/{id}.
    private readonly AlteracaoDeViagem alteracao = new(mapas, rotas);

    /// <inheritdoc />
    /// <remarks>
    /// Faz as mesmas verificações e o mesmo cálculo da alteração (<see cref="IViagemService.EditarAsync"/>), mas não
    /// grava nada nem notifica ninguém. D-2: pede a linha à HERE (fora do limite de 10 s) e guarda a rota simulada em
    /// memória durante 10 minutos (<see cref="IRotaService.ConcluirSimulacaoAsync"/>): o <c>PUT</c> seguinte com os
    /// mesmos locais reutiliza-a sem chamar os mapas. Sem HERE, a linha são retas (<c>GeometriaAproximada = true</c>).
    /// </remarks>
    public async Task<Resultado<RotaDaViagem>> SimularAlteracaoAsync(
        Guid viagemId,
        Guid utilizadorId,
        string? origem,
        string? destino,
        CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;

        if (AlteracaoDeViagem.ValidarQuemAltera(viagem, utilizadorId, DateTime.UtcNow) is { } recusado)
            return recusado;

        // Como no PUT: um local omitido mantém-se.
        string novaOrigem = origem?.Trim() ?? viagem.Origem;
        string novoDestino = destino?.Trim() ?? viagem.Destino;

        if (AlteracaoDeViagem.ValidarLocais(novaOrigem, novoDestino) is { } invalido)
            return invalido;

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        // R-B01: com os mesmos locais, a rota simulada é a guardada; os mapas só se chamam se ela estiver por recalcular.
        ResultadoRotaComPontos? rota = RotaGuardada.Ler(viagem);
        bool locaisMudam = !RegraMesmoLocal.MesmoTexto(novaOrigem, viagem.Origem) || !RegraMesmoLocal.MesmoTexto(novoDestino, viagem.Destino);

        if (rota is null || locaisMudam)
        {
            // O mesmo cálculo do PUT, com as paragens dos passageiros aceites (RF-F002-07, RF-F008-05).
            Resultado<AlteracaoDeViagem.NovaRota> calculada =
                await alteracao.CalcularRotaComNovosLocaisAsync(viagem, novaOrigem, novoDestino, aceites, ct);

            if (calculada.Erro is { } erro)
                return erro;

            // D-2: a linha da HERE junta-se fora do limite de 10 s e a simulação fica em memória para o PUT.
            rota = await rotas.ConcluirSimulacaoAsync(viagem, calculada.Valor.Rota, ct);
        }

        // O custo exige uma distância positiva (EstimativaCustoService).
        if (rota.DistanciaTotalKm <= 0)
            return ErrosDaViagem.RotaNaoCalculada;

        // O condutor vê todos os pontos; o custo é o dos passageiros aceites (RF-F008-10).
        return RespostasDaRota.RotaCompleta(viagem.Id, aceites.Count, rota, custos.Calcular(rota.DistanciaTotalKm, aceites.Count));
    }
}
