namespace CampusCarpool.Domain.Services;

public static class CalculadoraDeCusto
{
    public static decimal Calcular(decimal distanciaKm, decimal precoKm, int passageirosAceites)
    {
        if (distanciaKm < 0 || precoKm < 0 || passageirosAceites < 0) throw new ArgumentOutOfRangeException();
        return Math.Round(distanciaKm * precoKm / (passageirosAceites + 1), 2, MidpointRounding.AwayFromZero);
    }
}
