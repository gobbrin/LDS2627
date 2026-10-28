
using CampusCarpool.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class EstimativaCustoTests
{
    private static EstimativaCustoService CriarServico(
        decimal precoPorKm = 0.10m)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Custos:PrecoPorKm"] =
                        precoPorKm.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                })
            .Build();

        return new EstimativaCustoService(config);
    }

    [Fact]
    public void ExemploOficial_30Km_DoisPassageiros()
    {
        var servico = CriarServico();

        var resultado = servico.Calcular(30m, 2);

        Assert.Equal(3m, resultado.CustoTotalEstimado);
        Assert.Equal(1m, resultado.CustoPorParticipante);
        Assert.Equal(3, resultado.TotalParticipantes);
        Assert.True(resultado.Indicativo);
        Assert.Equal("EUR", resultado.Moeda);
    }

    [Fact]
    public void SemPassageiros_CondutorSuportaEstimativa()
    {
        var resultado = CriarServico().Calcular(20m, 0);

        Assert.Equal(2m, resultado.CustoPorParticipante);
        Assert.Equal(1, resultado.TotalParticipantes);
    }

    [Fact]
    public void TresPassageiros_DividePorQuatro()
    {
        var resultado = CriarServico().Calcular(40m, 3);

        Assert.Equal(1m, resultado.CustoPorParticipante);
        Assert.Equal(4, resultado.TotalParticipantes);
    }

    [Fact]
    public void MesmoInput_ProduzMesmoResultado()
    {
        var servico = CriarServico();

        var primeiro = servico.Calcular(35m, 2);
        var segundo = servico.Calcular(35m, 2);

        Assert.Equal(primeiro, segundo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DistanciaInvalida_DevolveErro(
        int distancia)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CriarServico().Calcular(distancia, 1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void PassageirosInvalidos_DevolveErro(
        int passageiros)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CriarServico().Calcular(30m, passageiros));
    }

    [Fact]
    public void PrecoNaoConfigurado_DevolveErro()
    {
        var config = new ConfigurationBuilder().Build();

        var servico = new EstimativaCustoService(config);

        Assert.Throws<InvalidOperationException>(() =>
            servico.Calcular(30m, 2));
    }

    [Fact]
    public void PrecoZero_DevolveErro()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CriarServico(0m).Calcular(30m, 2));
    }

    [Fact]
    public void AlterarParticipantes_AtualizaEstimativa()
    {
        var servico = CriarServico();

        var semPassageiros = servico.Calcular(30m, 0);
        var comDoisPassageiros = servico.Calcular(30m, 2);

        Assert.Equal(3m, semPassageiros.CustoPorParticipante);
        Assert.Equal(1m, comDoisPassageiros.CustoPorParticipante);
    }
}
