using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Rotas;

/// <summary>Ajudas para a linha da rota que os frontends desenham no mapa (RF-F008-07, DEF-BE-054).</summary>
public static class GeometriaRota
{
    /// <summary>
    /// Linha de reserva: retas entre os pontos, pela ordem da rota. Usa-se quando a HERE não deu a linha.
    /// </summary>
    /// <param name="pontos">Pontos da rota, por ordem.</param>
    /// <returns>As coordenadas dos pontos, pela mesma ordem.</returns>
    public static List<Coordenadas> LinhaReta(IReadOnlyList<PontoRota> pontos)
    {
        var linha = new List<Coordenadas>();

        foreach (PontoRota ponto in pontos)
            linha.Add(ponto.Coordenadas);

        return linha;
    }

    /// <summary>Converte a linha para o formato JSON da API e da coluna: pares <c>[latitude, longitude]</c>.</summary>
    public static List<decimal[]> EmPares(IReadOnlyList<Coordenadas> linha)
    {
        var pares = new List<decimal[]>();

        foreach (Coordenadas ponto in linha)
            pares.Add([ponto.Latitude, ponto.Longitude]);

        return pares;
    }

    /// <summary>Converte pares <c>[latitude, longitude]</c> (lidos da coluna) em coordenadas.</summary>
    /// <exception cref="InvalidOperationException">Um par sem exatamente dois números.</exception>
    public static List<Coordenadas> DePares(IReadOnlyList<decimal[]> pares)
    {
        var linha = new List<Coordenadas>();

        foreach (decimal[] par in pares)
        {
            if (par.Length != 2)
                throw new InvalidOperationException("Rota guardada inválida: ponto da linha sem latitude e longitude.");

            linha.Add(new Coordenadas(par[0], par[1]));
        }

        return linha;
    }

    /// <summary>Indica se duas rotas passam pelos mesmos pontos (mesmas coordenadas, pela mesma ordem).</summary>
    /// <remarks>Se sim, a linha guardada continua certa e não se pede outra à HERE (decisão de 16/11: guardar e reutilizar).</remarks>
    public static bool MesmosPontos(ResultadoRotaComPontos primeira, ResultadoRotaComPontos segunda)
    {
        if (primeira.PontosOrdenados.Count != segunda.PontosOrdenados.Count)
            return false;

        for (int i = 0; i < primeira.PontosOrdenados.Count; i++)
        {
            if (primeira.PontosOrdenados[i].Coordenadas != segunda.PontosOrdenados[i].Coordenadas)
                return false;
        }

        return true;
    }
}
