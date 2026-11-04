using CampusCarpool.Domain.Services;
using Xunit;
namespace CampusCarpool.Tests.Unit;

/// <summary>Exemplo base da <c>CalculadoraDeCusto</c> (P-06): condutor e dois passageiros; distância negativa.</summary>
public sealed class CalculadoraDeCustoTests
{
    [Fact] public void DividePeloCondutorEMaisDoisPassageiros() => Assert.Equal(1.00m, CalculadoraDeCusto.Calcular(30m, 0.10m, 2));
    [Fact] public void RejeitaDistanciaNegativa() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraDeCusto.Calcular(-1m, 0.10m, 2));
}
