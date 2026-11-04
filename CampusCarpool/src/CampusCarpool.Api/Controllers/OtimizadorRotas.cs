using CampusCarpool.Api.Services;

namespace CampusCarpool.Api.Controllers;

/// <summary>Pontos efetivos de um passageiro (já com P-20 aplicado: nunca vazios).</summary>
public sealed record ParagemPassageiro(string Recolha, string Destino);
/// <summary>Rota otimizada: distância (km, 2 casas), duração (minutos, arredondada para cima) e nomes dos pontos por ordem.</summary>
public sealed record RotaOtimizada(decimal DistanciaKm, int DuracaoMinutos, IReadOnlyList<string> Ordem);

/// <summary>Programação dinâmica exata sobre sequências com precedências recolha -> destino.</summary>
/// <remarks>
/// Cumpre P-07: menor duração total, com a recolha de cada passageiro antes do seu destino (RF-F004-04).
/// Só é usado pela simulação; a consulta da rota usa <c>OrdenadorPontosRota</c> (DEF-BE-022, fica para m4).
/// Está na pasta <c>Controllers/</c>, embora não seja um controller.
/// </remarks>
public static class OtimizadorRotas
{
    /// <summary>Calcula a ordem ótima das paragens entre a origem e o destino fixos.</summary>
    /// <remarks>
    /// Algoritmo (estilo Held-Karp): os pontos intermédios são numerados 1..n; o estado é (conjunto de pontos já
    /// visitados como máscara de bits, último ponto). Um destino só pode entrar no conjunto depois da respetiva
    /// recolha. Complexidade O(2^n · n²), aceitável porque n ≤ 16 (8 passageiros × 2 pontos).
    /// Faz um pedido de geocodificação por ponto e um pedido à Matrix API para todas as durações e distâncias.
    /// </remarks>
    /// <exception cref="ArgumentException">Mais de 8 passageiros, pontos vazios ou recolha igual ao destino.</exception>
    /// <exception cref="InvalidOperationException">Local não encontrado, matriz indisponível ou nenhuma sequência viável.</exception>
    public static async Task<RotaOtimizada> CalcularAsync(
        MapasService mapas, string origem, string destino,
        IReadOnlyList<ParagemPassageiro> passageiros, CancellationToken ct)
    {
        if (passageiros.Count > 8) throw new ArgumentException("Máximo de oito passageiros.");
        // A origem e o destino são fixos; as paragens intermédias são otimizadas.
        // Pontos iguais à origem/destino não geram paragens redundantes.
        var nomes = new List<string> { origem };
        var prerequisitos = new List<int>();
        foreach (var p in passageiros)
        {
            if (string.IsNullOrWhiteSpace(p.Recolha) || string.IsNullOrWhiteSpace(p.Destino))
                throw new ArgumentException("Os pontos efetivos são obrigatórios.");
            if (string.Equals(p.Recolha, p.Destino, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Recolha e destino não podem coincidir.");
            var recolhaIndex = -1;
            if (!string.Equals(p.Recolha, origem, StringComparison.OrdinalIgnoreCase))
            {
                recolhaIndex = prerequisitos.Count;
                nomes.Add(p.Recolha);
                prerequisitos.Add(-1);
            }
            if (!string.Equals(p.Destino, destino, StringComparison.OrdinalIgnoreCase))
            {
                nomes.Add(p.Destino);
                prerequisitos.Add(recolhaIndex);
            }
        }
        nomes.Add(destino);
        var n = prerequisitos.Count;
        var pontos = new List<Coordenadas>(nomes.Count);
        foreach (var nome in nomes)
        {
            ct.ThrowIfCancellationRequested();
            var coord = await mapas.GeocodificarAsync(nome, ct);
            if (coord is null) throw new InvalidOperationException($"Localização não encontrada: {nome}");
            pontos.Add(coord);
        }
        // Matrix API: uma chamada para obter tempos e distâncias entre todos os pares.
        var matriz = await mapas.CalcularMatrizAsync(pontos, ct);
        if (matriz is null) throw new InvalidOperationException("Matriz de rotas indisponível.");
        // Índices na matriz: 0 = origem, 1..n = pontos intermédios, n + 1 = destino.
        // dp[máscara, último] = menor duração (s) para visitar os pontos da máscara terminando em "último";
        // distancia guarda a distância (m) dessa melhor sequência e anterior permite reconstruí-la.
        var total = 1 << n;
        var dp = new double[total, n + 1];
        var distancia = new double[total, n + 1];
        var anterior = new int[total, n + 1];
        for (var m = 0; m < total; m++)
            for (var j = 0; j <= n; j++) { dp[m, j] = double.PositiveInfinity; anterior[m, j] = -1; }
        dp[0, 0] = 0;
        for (var mask = 0; mask < total; mask++)
            for (var last = 0; last <= n; last++)
            {
                if (double.IsPositiveInfinity(dp[mask, last])) continue;
                for (var k = 0; k < n; k++)
                {
                    if ((mask & (1 << k)) != 0) continue;
                    // Precedência: o destino k só pode ser visitado depois da recolha req.
                    var req = prerequisitos[k];
                    if (req >= 0 && (mask & (1 << req)) == 0) continue;
                    var next = k + 1;
                    var seg = matriz.DuracoesSegundos[last][next];
                    if (seg is null) continue;
                    var candidate = dp[mask, last] + seg.Value;
                    if (candidate >= dp[mask | (1 << k), next]) continue;
                    dp[mask | (1 << k), next] = candidate;
                    distancia[mask | (1 << k), next] = distancia[mask, last] + matriz.DistanciasMetros[last][next]!.Value;
                    anterior[mask | (1 << k), next] = last;
                }
            }
        var fim = total - 1;
        var melhor = double.PositiveInfinity;
        var melhorUltimo = -1;
        for (var j = 0; j <= n; j++)
        {
            var seg = matriz.DuracoesSegundos[j][n + 1];
            if (seg is null) continue;
            var valor = dp[fim, j] + seg.Value;
            if (valor < melhor) { melhor = valor; melhorUltimo = j; }
        }
        if (melhorUltimo < 0 || double.IsPositiveInfinity(melhor))
            throw new InvalidOperationException("Não existe uma sequência de paragens viável.");
        var metros = distancia[fim, melhorUltimo] + matriz.DistanciasMetros[melhorUltimo][n + 1]!.Value;
        // Reconstrói a sequência do fim para o início, seguindo "anterior", e inverte-a no fim.
        var ordem = new List<string> { destino };
        var atual = melhorUltimo;
        var estado = fim;
        while (atual > 0)
        {
            ordem.Add(nomes[atual]);
            var prev = anterior[estado, atual];
            estado &= ~(1 << (atual - 1));
            atual = prev;
        }
        ordem.Add(origem);
        ordem.Reverse();
        return new RotaOtimizada(Math.Round((decimal)(metros / 1000d), 2), (int)Math.Ceiling(melhor / 60d), ordem);
    }
}
