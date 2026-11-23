using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Descodificação da «Flexible Polyline» da HERE (DEF-BE-054, I112).</summary>
public sealed class PolilinhaFlexivelTests
{
    [Fact]
    /// <summary>Exemplo da documentação da HERE: 4 pontos com precisão 5, com diferenças positivas e negativas.</summary>
    public void Descodificar_ExemploDaDocumentacao_DevolveOsQuatroPontos()
    {
        List<Coordenadas> pontos = PolilinhaFlexivel.Descodificar(HereFalsa.Polilinha);

        Assert.Equal(
            [
                new Coordenadas(50.10228m, 8.69821m),
                new Coordenadas(50.10201m, 8.69567m),
                new Coordenadas(50.10063m, 8.69150m),
                new Coordenadas(50.09878m, 8.68752m)
            ],
            pontos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CF")] // versão 2
    [InlineData("BFoz5x")] // número cortado a meio
    [InlineData("BFoz5xJ67i1B1B7Pz!haxL7Y")] // carácter fora do alfabeto
    public void Descodificar_TextoInvalido_LancaFormatException(string texto)
    {
        Assert.Throws<FormatException>(() => PolilinhaFlexivel.Descodificar(texto));
    }
}
