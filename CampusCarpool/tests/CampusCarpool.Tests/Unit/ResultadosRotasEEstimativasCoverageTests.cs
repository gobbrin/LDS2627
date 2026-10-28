using CampusCarpool.Api.Services;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class ResultadosRotasEEstimativasCoverageTests
{
    [Fact]
    public void SegmentoRota_ExpoeTodosOsDados()
    {
        var origem = new PontoRota(Guid.NewGuid(), "recolha", "Porto", new Coordenadas(41.1m, -8.6m));
        var destino = new PontoRota(null, "destino", "Braga", new Coordenadas(41.5m, -8.4m));
        var segmento = new SegmentoRota(origem, destino, 55.4m, 49);
        Assert.Same(origem, segmento.Origem);
        Assert.Same(destino, segmento.Destino);
        Assert.Equal(55.4m, segmento.DistanciaKm);
        Assert.Equal(49, segmento.DuracaoMinutos);
        Assert.Equal("Porto", segmento.Origem.Local);
    }

    [Fact]
    public void ResultadoEstimativa_ExpoePrecoEPassageiros()
    {
        var resultado = new ResultadoEstimativaCusto(20m, 0.12m, 2, 3, 2.4m, 0.8m, true, "EUR");
        Assert.Equal(0.12m, resultado.PrecoPorKm);
        Assert.Equal(2, resultado.PassageirosAceites);
    }
}
