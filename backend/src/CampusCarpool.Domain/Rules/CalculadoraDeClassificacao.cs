namespace CampusCarpool.Domain.Rules;

/// <summary>Classificação média de um utilizador (P-10).</summary>
public static class CalculadoraDeClassificacao
{
    /// <summary>Média das classificações, com uma casa decimal; <c>null</c> sem avaliações.</summary>
    public static decimal? Media(IEnumerable<byte> classificacoes)
    {
        var valores = classificacoes.ToList();

        return valores.Count == 0
            ? null
            : Math.Round(valores.Average(c => (decimal)c), 1, MidpointRounding.AwayFromZero);
    }
}
