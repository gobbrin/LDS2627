using CampusCarpool.Api.Services;

namespace CampusCarpool.Api.Controllers;

public sealed record ParagemPassageiro(string Recolha, string Destino);
public sealed record RotaOtimizada(decimal DistanciaKm, int DuracaoMinutos, IReadOnlyList<string> Ordem);

/// <summary>Programação dinâmica exata sobre sequências com precedências recolha -> destino.</summary>
public static class OtimizadorRotas
{
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
        var total = 1 << n;
        var dp = new double[total, n + 1];
        var distancia = new double[total, n + 1];
        var anterior = new int[total, n + 1];
        for (var m = 0; m < total; m++)
            for (var j = 0; j <= n; j++) { dp[m,j] = double.PositiveInfinity; anterior[m,j] = -1; }
        dp[0,0] = 0;
        for (var mask = 0; mask < total; mask++)
            for (var last = 0; last <= n; last++)
            {
                if (double.IsPositiveInfinity(dp[mask,last])) continue;
                for (var k = 0; k < n; k++)
                {
                    if ((mask & (1 << k)) != 0) continue;
                    var req = prerequisitos[k];
                    if (req >= 0 && (mask & (1 << req)) == 0) continue;
                    var next = k + 1;
                    var seg = matriz.DuracoesSegundos[last][next];
                    if (seg is null) continue;
                    var candidate = dp[mask,last] + seg.Value;
                    if (candidate >= dp[mask | (1 << k),next]) continue;
                    dp[mask | (1 << k),next] = candidate;
                    distancia[mask | (1 << k),next] = distancia[mask,last] + matriz.DistanciasMetros[last][next]!.Value;
                    anterior[mask | (1 << k),next] = last;
                }
            }
        var fim = total - 1;
        var melhor = double.PositiveInfinity;
        var melhorUltimo = -1;
        for (var j = 0; j <= n; j++)
        {
            var seg = matriz.DuracoesSegundos[j][n + 1];
            if (seg is null) continue;
            var valor = dp[fim,j] + seg.Value;
            if (valor < melhor) { melhor = valor; melhorUltimo = j; }
        }
        if (melhorUltimo < 0 || double.IsPositiveInfinity(melhor))
            throw new InvalidOperationException("Não existe uma sequência de paragens viável.");
        var metros = distancia[fim,melhorUltimo] + matriz.DistanciasMetros[melhorUltimo][n + 1]!.Value;
        var ordem = new List<string> { destino };
        var atual = melhorUltimo;
        var estado = fim;
        while (atual > 0)
        {
            ordem.Add(nomes[atual]);
            var prev = anterior[estado,atual];
            estado &= ~(1 << (atual - 1));
            atual = prev;
        }
        ordem.Add(origem);
        ordem.Reverse();
        return new RotaOtimizada(Math.Round((decimal)(metros / 1000d),2), (int)Math.Ceiling(melhor / 60d), ordem);
    }
}
