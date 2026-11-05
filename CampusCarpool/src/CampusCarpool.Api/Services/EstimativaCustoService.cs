
namespace CampusCarpool.Api.Services;

using CampusCarpool.Domain.Rules;

/// <summary>Estimativa de custo devolvida ao cliente (RF-F008-10, RF-F008-11).</summary>
/// <remarks><c>Indicativo</c> é sempre <c>true</c>: o valor é só uma referência para dividir despesas, não um preço.</remarks>
public sealed record ResultadoEstimativaCusto(
    decimal DistanciaKm,
    decimal PrecoPorKm,
    int PassageirosAceites,
    int TotalParticipantes,
    decimal CustoTotalEstimado,
    decimal CustoPorParticipante,
    bool Indicativo,
    string Moeda
);

/// <summary>Junta a configuração (<c>Custos:PrecoPorKm</c>) ao cálculo do Domain (<see cref="CalculadoraDeCusto"/>, P-06).</summary>
public sealed class EstimativaCustoService(
    IConfiguration config)
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

        var precoPorKm = config.GetValue<decimal?>(
            "Custos:PrecoPorKm");

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
