using CampusCarpool.Domain.Rules;
using Microsoft.Extensions.Options;

namespace CampusCarpool.Application.Rotas;

/// <summary>Junta a configuração (<see cref="OpcoesCustos"/>) ao cálculo do Domain (<see cref="CalculadoraDeCusto"/>, P-06).</summary>
public sealed class EstimativaCustoService(IOptions<OpcoesCustos> opcoes)
{
    /// <summary>Calcula o custo total e por participante (condutor incluído).</summary>
    /// <param name="distanciaKm">Distância total da rota; tem de ser positiva.</param>
    /// <param name="passageirosAceites">Passageiros aceites, de 0 a 8 (P-11).</param>
    /// <exception cref="ArgumentOutOfRangeException">Distância não positiva ou passageiros fora de 0 a 8.</exception>
    /// <exception cref="InvalidOperationException">Preço por km em falta ou não positivo na configuração.</exception>
    public ResultadoEstimativaCusto Calcular(
        decimal distanciaKm,
        int passageirosAceites)
    {
        if (distanciaKm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanciaKm),
                "A distância deve ser superior a zero.");
        }

        if (passageirosAceites < 0 || passageirosAceites > 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(passageirosAceites),
                "O número de passageiros aceites é inválido.");
        }

        var precoPorKm = opcoes.Value.PrecoPorKm;

        if (!precoPorKm.HasValue || precoPorKm.Value <= 0)
        {
            throw new InvalidOperationException(
                "O preço por quilómetro não está configurado corretamente.");
        }

        var totalParticipantes = passageirosAceites + 1;

        var custoTotal = distanciaKm * precoPorKm.Value;

        var custoPorParticipante = CalculadoraDeCusto.Calcular(
    distanciaKm,
    precoPorKm.Value,
    passageirosAceites);

        return new ResultadoEstimativaCusto(
            distanciaKm,
            precoPorKm.Value,
            passageirosAceites,
            totalParticipantes,
            Math.Round(custoTotal, 2, MidpointRounding.AwayFromZero),
            custoPorParticipante,
            true,
            "EUR"
        );
    }
}
