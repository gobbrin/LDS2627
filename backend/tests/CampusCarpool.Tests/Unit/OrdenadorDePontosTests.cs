using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Regra pura de ordenação (P-07, RF-F004-04), sem Serviço de Mapas: a matriz é dada pelo teste.</summary>
public sealed class OrdenadorDePontosTests
{
    private static readonly Guid Passageiro = Guid.NewGuid();

    private static PontoRota Ponto(string tipo, Guid? participacao = null) =>
        new(participacao, tipo, tipo, new Coordenadas(41m, -8m));

    [Fact]
    public void SemPontosAdicionais_DevolveOrigemEDestino()
    {
        double?[][] duracoes = [[0, 60], [60, 0]];

        Assert.Equal([0, 1], OrdenadorDePontos.Ordenar(duracoes, []));
    }

    [Fact]
    public void EscolheAMenorDuracaoTotal()
    {
        // Índices: 0 origem, 1 e 2 adicionais, 3 destino. O caminho 0 → 2 → 1 → 3 é o mais curto.
        double?[][] duracoes =
        [
            [0, 60, 120, 5000],
            [5000, 0, 900, 60],
            [5000, 60, 0, 60],
            [5000, 5000, 5000, 0],
        ];

        var ordem = OrdenadorDePontos.Ordenar(duracoes, [Ponto(TiposPontoRota.Recolha), Ponto(TiposPontoRota.Recolha)]);

        Assert.Equal([0, 2, 1, 3], ordem);
    }

    [Fact]
    public void DestinoDoPassageiroVemDepoisDaRecolha_MesmoSendoMaisLento()
    {
        var destino = Ponto(TiposPontoRota.DestinoPassageiro, Passageiro);
        var recolha = Ponto(TiposPontoRota.Recolha, Passageiro);

        // Índices: 1 destino do passageiro, 2 recolha. Passar primeiro pelo destino seria muito mais rápido.
        double?[][] duracoes =
        [
            [0, 1, 1000, 1000],
            [1000, 0, 1, 1],
            [1000, 1000, 0, 1000],
            [1000, 1000, 1000, 0],
        ];

        Assert.Equal([0, 2, 1, 3], OrdenadorDePontos.Ordenar(duracoes, [destino, recolha]));
    }

    [Fact]
    public void SemNenhumaSequenciaViavel_DevolveNull()
    {
        // O ponto adicional não tem ligação a partir da origem.
        double?[][] duracoes =
        [
            [0, null, 60],
            [60, 0, 60],
            [60, 60, 0],
        ];

        Assert.Null(OrdenadorDePontos.Ordenar(duracoes, [Ponto(TiposPontoRota.Recolha)]));
    }

    [Fact]
    public void MaisDe16PontosAdicionais_ERecusado()
    {
        var pontos = Enumerable.Range(0, 17).Select(_ => Ponto(TiposPontoRota.Recolha)).ToList();

        Assert.Throws<ArgumentException>(() => OrdenadorDePontos.GarantirLimite(pontos));
        Assert.Throws<ArgumentException>(() => OrdenadorDePontos.Ordenar([], pontos));
    }

    [Fact]
    public void Exatamente16PontosAdicionais_EAceite()
    {
        var pontos = Enumerable.Range(0, 16).Select(_ => Ponto(TiposPontoRota.Recolha)).ToArray();

        OrdenadorDePontos.GarantirLimite(pontos);
    }
}
