using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>
/// Ajudas puras da linha da rota (RF-F008-07, DEF-BE-054): leitura da linha guardada e comparação de pontos, que
/// decide se a linha guardada se reutiliza sem pedir outra à HERE (decisão de 16/11).
/// </summary>
/// <remarks>O caminho com base de dados e HERE está em <c>Integration/GeometriaRotaTests</c>.</remarks>
public sealed class GeometriaRotaUnitTests
{
    /// <summary>Ponto de recolha nas coordenadas indicadas.</summary>
    private static PontoRota Ponto(decimal latitude, decimal longitude) =>
        new(null, TiposPontoRota.Recolha, "Local", new Coordenadas(latitude, longitude));

    /// <summary>Rota só com os pontos (sem segmentos nem totais), que é o que a comparação usa.</summary>
    private static ResultadoRotaComPontos Rota(params PontoRota[] pontos) => new(pontos, [], 0m, 0);

    /// <summary>Uma linha guardada converte-se para pares e volta às mesmas coordenadas, pela mesma ordem.</summary>
    [Fact]
    public void I112_39_EmParesEDePares_DevolvemAMesmaLinha()
    {
        List<Coordenadas> linha = [new(41.15m, -8.61m), new(41.55m, -8.42m)];

        List<decimal[]> pares = GeometriaRota.EmPares(linha);

        Assert.Equal(linha, GeometriaRota.DePares(pares));
    }

    /// <summary>Um par sem exatamente latitude e longitude na coluna é um erro da rota guardada (não se adivinha).</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void I112_40_DePares_ParSemDoisNumeros_LancaErro(int numeros)
    {
        List<decimal[]> pares = [new decimal[numeros]];

        var erro = Assert.Throws<InvalidOperationException>(() => GeometriaRota.DePares(pares));
        Assert.Equal("Rota guardada inválida: ponto da linha sem latitude e longitude.", erro.Message);
    }

    /// <summary>Os mesmos pontos pela mesma ordem: a linha guardada continua certa.</summary>
    [Fact]
    public void I112_41_MesmosPontos_MesmaOrdem_DevolveVerdadeiro()
    {
        var primeira = Rota(Ponto(41.15m, -8.61m), Ponto(41.55m, -8.42m));
        var segunda = Rota(Ponto(41.15m, -8.61m), Ponto(41.55m, -8.42m));

        Assert.True(GeometriaRota.MesmosPontos(primeira, segunda));
    }

    /// <summary>Uma paragem a mais, a mesma paragem noutra ordem ou noutro sítio: é preciso outra linha.</summary>
    [Fact]
    public void I112_42_MesmosPontos_NumeroOrdemOuCoordenadasDiferentes_DevolveFalso()
    {
        var original = Rota(Ponto(41.15m, -8.61m), Ponto(41.55m, -8.42m));

        Assert.False(GeometriaRota.MesmosPontos(original, Rota(Ponto(41.15m, -8.61m))));
        Assert.False(GeometriaRota.MesmosPontos(original, Rota(Ponto(41.55m, -8.42m), Ponto(41.15m, -8.61m))));
        Assert.False(GeometriaRota.MesmosPontos(original, Rota(Ponto(41.15m, -8.61m), Ponto(41.23m, -8.62m))));
    }
}
