namespace CampusCarpool.Domain.Services;

/// <summary>Regra de «mesmo local» do glossário (D-008), usada em RF-F002-04, RF-F002-16, RF-F004-18, RF-F004-20 e RF-F004-21.</summary>
/// <remarks>
/// Dois locais são o mesmo quando o texto é igual, sem distinguir maiúsculas e ignorando espaços nas extremidades,
/// ou quando as coordenadas são iguais. Locais iguais escritos de formas diferentes e geocodificados para pontos
/// diferentes não são detetados: limitação conhecida do MVP.
/// </remarks>
public static class RegraMesmoLocal
{
    // Precisão das colunas de coordenadas (decimal(9,6)): duas coordenadas iguais na BD têm de dar igual aqui.
    private const int CasasDecimais = 6;

    /// <summary>Compara só o texto, para recusar o pedido antes de chamar o Serviço de Mapas.</summary>
    public static bool MesmoTexto(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Compara as coordenadas, arredondadas à precisão com que são guardadas.</summary>
    public static bool MesmasCoordenadas(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB) =>
        Math.Round(latitudeA, CasasDecimais) == Math.Round(latitudeB, CasasDecimais)
        && Math.Round(longitudeA, CasasDecimais) == Math.Round(longitudeB, CasasDecimais);
}
