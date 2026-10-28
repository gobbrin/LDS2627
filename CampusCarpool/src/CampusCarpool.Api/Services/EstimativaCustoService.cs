
namespace CampusCarpool.Api.Services;

using CampusCarpool.Domain.Services;

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

public sealed class EstimativaCustoService(
    IConfiguration config)
{
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
