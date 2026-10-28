
using CampusCarpool.Api.Services;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public class OrdenadorPontosRotaTests
{
    private static PontoRota Ponto(
        Guid? id,
        string tipo,
        decimal latitude,
        decimal longitude)
    {
        return new PontoRota(
            id,
            tipo,
            tipo,
            new Coordenadas(latitude, longitude));
    }

    [Fact]
    public void SemPontosAdicionais_DevolveOrigemEDestino()
    {
        var origem = Ponto(null, "Origem", 40, -8);
        var destino = Ponto(null, "DestinoViagem", 41, -8);

        var resultado = OrdenadorPontosRota.Ordenar(
            origem,
            [],
            destino);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(origem, resultado[0]);
        Assert.Equal(destino, resultado[1]);
    }

    [Fact]
    public void RecolhaAconteceAntesDoDestinoDoPassageiro()
    {
        var id = Guid.NewGuid();
        var origem = Ponto(null, "Origem", 40, -8);
        var destino = Ponto(null, "DestinoViagem", 41, -8);

        var recolha = Ponto(id, "Recolha", 40.5m, -8);
        var destinoPassageiro = Ponto(id, "DestinoPassageiro", 40.1m, -8);

        var resultado = OrdenadorPontosRota.Ordenar(
            origem,
            [destinoPassageiro, recolha],
            destino);

        Assert.True(
            resultado.IndexOf(recolha) <
            resultado.IndexOf(destinoPassageiro));
    }

    [Fact]
    public void VariosPassageiros_RespeitaTodasAsRecolhas()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var origem = Ponto(null, "Origem", 40, -8);
        var destino = Ponto(null, "DestinoViagem", 42, -8);

        var recolhaA = Ponto(idA, "Recolha", 40.2m, -8);
        var destinoA = Ponto(idA, "DestinoPassageiro", 40.7m, -8);
        var recolhaB = Ponto(idB, "Recolha", 40.4m, -8);
        var destinoB = Ponto(idB, "DestinoPassageiro", 41.5m, -8);

        var resultado = OrdenadorPontosRota.Ordenar(
            origem,
            [destinoA, recolhaB, destinoB, recolhaA],
            destino);

        Assert.Equal(6, resultado.Count);

        Assert.True(
            resultado.IndexOf(recolhaA) <
            resultado.IndexOf(destinoA));

        Assert.True(
            resultado.IndexOf(recolhaB) <
            resultado.IndexOf(destinoB));
    }

    [Fact]
    public void DestinoSemRecolha_ConsideraRecolhaNaOrigem()
    {
        var id = Guid.NewGuid();
        var origem = Ponto(null, "Origem", 40, -8);
        var destino = Ponto(null, "DestinoViagem", 41, -8);
        var destinoPassageiro = Ponto(
            id, "DestinoPassageiro", 40.5m, -8);

        var resultado = OrdenadorPontosRota.Ordenar(
            origem,
            [destinoPassageiro],
            destino);

        Assert.Equal(3, resultado.Count);
        Assert.Equal(origem, resultado[0]);
        Assert.Equal(destinoPassageiro, resultado[1]);
        Assert.Equal(destino, resultado[2]);
    }
}
