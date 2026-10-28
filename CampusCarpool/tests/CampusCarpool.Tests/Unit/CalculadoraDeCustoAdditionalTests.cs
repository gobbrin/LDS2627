using CampusCarpool.Domain.Services;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class CalculadoraDeCustoAdditionalTests
{
    [Theory]
    [InlineData(0, 0.10, 0, 0)]
    [InlineData(10, 0, 2, 0)]
    [InlineData(10, 0.20, 0, 2)]
    [InlineData(10, 0.20, 1, 1)]
    [InlineData(30, 0.10, 2, 1)]
    public void CalculaValorPorOcupante(double distancia, double preco, int passageiros, double esperado)
    {
        Assert.Equal((decimal)esperado, CalculadoraDeCusto.Calcular((decimal)distancia, (decimal)preco, passageiros));
    }

    [Fact]
    public void ArredondaMetadeParaLongeDeZero()
    {
        Assert.Equal(0.01m, CalculadoraDeCusto.Calcular(1m, 0.005m, 0));
    }

    [Theory]
    [InlineData(-1, 1, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 1, -1)]
    public void RejeitaArgumentosNegativos(double distancia, double preco, int passageiros)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculadoraDeCusto.Calcular((decimal)distancia, (decimal)preco, passageiros));
    }
}
