using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Domain.Rules;

/// <summary>Ordena os pontos de uma rota pela menor duração total, com a recolha antes do destino de cada passageiro.</summary>
/// <remarks>
/// Cumpre P-07 e RF-F004-04 com programação dinâmica exata (estilo Held-Karp). É o único algoritmo de ordenação: a
/// rota guardada e a simulação usam-no, por isso a rota depois de aceitar coincide com a simulada (DEF-BE-022).
/// </remarks>
public static class OrdenadorDePontos
{
    // 8 passageiros (P-11) com 2 pontos cada.
    private const int MaximoPontosAdicionais = 16;

    /// <summary>Recusa mais pontos adicionais do que os possíveis numa viagem, antes de se pedir a matriz.</summary>
    /// <exception cref="ArgumentException">Mais de 16 pontos adicionais.</exception>
    public static void GarantirLimite(IReadOnlyCollection<PontoRota> adicionais)
    {
        ArgumentNullException.ThrowIfNull(adicionais);

        if (adicionais.Count > MaximoPontosAdicionais)
            throw new ArgumentException("Máximo de oito passageiros.", nameof(adicionais));
    }

    /// <summary>Calcula a ordem ótima dos pontos adicionais entre a origem e o destino da viagem.</summary>
    /// <remarks>
    /// Os pontos intermédios são numerados 1..n; o estado é (conjunto de pontos já visitados como máscara de bits,
    /// último ponto). Um destino só pode entrar no conjunto depois da respetiva recolha; o destino de um passageiro
    /// sem recolha própria entra na origem da viagem. Complexidade O(2^n · n²), aceitável porque n ≤ 16.
    /// </remarks>
    /// <param name="duracoesSegundos">
    /// Matriz de durações: índice 0 = origem, 1..n = pontos adicionais, n + 1 = destino; <c>null</c> se não houver rota.
    /// </param>
    /// <param name="adicionais">Pontos adicionais, pela mesma ordem da matriz.</param>
    /// <returns>A sequência completa de índices da matriz; <c>null</c> se não houver nenhuma sequência viável.</returns>
    /// <exception cref="ArgumentException">Mais de 16 pontos adicionais.</exception>
    public static IReadOnlyList<int>? Ordenar(double?[][] duracoesSegundos, IReadOnlyList<PontoRota> adicionais)
    {
        ArgumentNullException.ThrowIfNull(duracoesSegundos);
        GarantirLimite(adicionais);

        return MelhorOrdem(duracoesSegundos, Prerequisitos(adicionais));
    }

    // Para cada ponto adicional, o índice (entre os adicionais) que tem de o preceder, ou -1 se não houver.
    private static int[] Prerequisitos(IReadOnlyList<PontoRota> adicionais)
    {
        var prerequisitos = new int[adicionais.Count];

        for (var i = 0; i < adicionais.Count; i++)
        {
            prerequisitos[i] = -1;
            if (adicionais[i].Tipo != TiposPontoRota.DestinoPassageiro) continue;

            for (var j = 0; j < adicionais.Count; j++)
            {
                if (adicionais[j].Tipo == TiposPontoRota.Recolha
                    && adicionais[j].ParticipacaoId == adicionais[i].ParticipacaoId)
                {
                    prerequisitos[i] = j;
                }
            }
        }

        return prerequisitos;
    }

    private static List<int>? MelhorOrdem(double?[][] duracoesSegundos, int[] prerequisitos)
    {
        var n = prerequisitos.Length;
        var total = 1 << n;

        // duracao[máscara, último] = menor duração (s) para visitar os pontos da máscara terminando em "último";
        // anterior permite reconstruir a sequência.
        var duracao = new double[total, n + 1];
        var anterior = new int[total, n + 1];

        for (var m = 0; m < total; m++)
        {
            for (var j = 0; j <= n; j++)
            {
                duracao[m, j] = double.PositiveInfinity;
                anterior[m, j] = -1;
            }
        }

        duracao[0, 0] = 0;

        for (var mascara = 0; mascara < total; mascara++)
        {
            for (var ultimo = 0; ultimo <= n; ultimo++)
            {
                if (double.IsPositiveInfinity(duracao[mascara, ultimo])) continue;

                for (var k = 0; k < n; k++)
                {
                    if ((mascara & (1 << k)) != 0) continue;

                    // Precedência: o destino k só pode ser visitado depois da respetiva recolha.
                    var requisito = prerequisitos[k];
                    if (requisito >= 0 && (mascara & (1 << requisito)) == 0) continue;

                    var segmento = duracoesSegundos[ultimo][k + 1];
                    if (segmento is null) continue;

                    var seguinte = mascara | (1 << k);
                    var candidato = duracao[mascara, ultimo] + segmento.Value;
                    if (candidato >= duracao[seguinte, k + 1]) continue;

                    duracao[seguinte, k + 1] = candidato;
                    anterior[seguinte, k + 1] = ultimo;
                }
            }
        }

        var cheia = total - 1;
        var melhor = double.PositiveInfinity;
        var melhorUltimo = -1;

        for (var j = 0; j <= n; j++)
        {
            var segmento = duracoesSegundos[j][n + 1];
            if (segmento is null) continue;

            var valor = duracao[cheia, j] + segmento.Value;
            if (valor < melhor)
            {
                melhor = valor;
                melhorUltimo = j;
            }
        }

        if (melhorUltimo < 0) return null;

        // Reconstrói a sequência do fim para o início e inverte-a.
        var ordem = new List<int> { n + 1 };
        var atual = melhorUltimo;
        var estado = cheia;

        while (atual > 0)
        {
            ordem.Add(atual);
            var previo = anterior[estado, atual];
            estado &= ~(1 << (atual - 1));
            atual = previo;
        }

        ordem.Add(0);
        ordem.Reverse();
        return ordem;
    }
}
