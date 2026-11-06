using System.Globalization;
using System.Text;

namespace CampusCarpool.Domain.Rules;

/// <summary>Normalização do texto para a pesquisa por origem e destino (P-12, D-009).</summary>
public static class TextoPesquisa
{
    /// <summary>Retira os acentos e passa a maiúsculas, para «Famalicao» encontrar «Famalicão».</summary>
    public static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
        var semAcentos = new StringBuilder(decomposto.Length);

        foreach (var caracter in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                semAcentos.Append(caracter);
        }

        return semAcentos.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
