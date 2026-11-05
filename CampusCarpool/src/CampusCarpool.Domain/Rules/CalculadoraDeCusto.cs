namespace CampusCarpool.Domain.Rules;

/// <summary>
/// Estimativa do custo da deslocação por participante (P-06, RF-F008-10, AD-002 §9).
/// </summary>
/// <remarks>
/// Valor indicativo (RF-F008-11). O preço por km vem da configuração <c>Custos:PrecoPorKm</c> (0,10 por
/// omissão) e a distância é escolhida por <c>EstimativaCustoService</c> (Api).
/// </remarks>
public static class CalculadoraDeCusto
{
    /// <summary>
    /// Calcula <c>distância × preço por km ÷ (passageiros aceites + 1)</c>, arredondado a 2 casas
    /// decimais (metades afastadas de zero). O <c>+ 1</c> conta o condutor como participante.
    /// </summary>
    /// <param name="distanciaKm">Distância total da rota, em km.</param>
    /// <param name="precoKm">Preço por km.</param>
    /// <param name="passageirosAceites">Número de participações aceites.</param>
    /// <returns>Custo por participante.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Algum dos argumentos é negativo.</exception>
    public static decimal Calcular(decimal distanciaKm, decimal precoKm, int passageirosAceites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(distanciaKm);
        ArgumentOutOfRangeException.ThrowIfNegative(precoKm);
        ArgumentOutOfRangeException.ThrowIfNegative(passageirosAceites);

        return Math.Round(distanciaKm * precoKm / (passageirosAceites + 1), 2, MidpointRounding.AwayFromZero);
    }
}
