using CampusCarpool.Api.Hubs;
using CampusCarpool.Tests.Integration;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Limite de bilhetes do hub guardados em memória e limpeza dos expirados (DEF-BE-055).</summary>
public sealed class BilhetesHubTests
{
    /// <summary>
    /// Com o máximo de bilhetes por usar ainda válidos, não se emite mais nenhum; depois de expirarem, a emissão
    /// seguinte limpa-os e volta a funcionar.
    /// </summary>
    [Fact]
    public void I112_122_LimiteDeBilhetes_RecusaEDepoisLimpaOsExpirados()
    {
        var relogio = new RelogioDeTeste();
        var bilhetes = new BilhetesHub(relogio);

        for (int i = 0; i < BilhetesHub.MaximoDeBilhetes; i++)
            Assert.NotNull(bilhetes.Emitir("jwt"));

        Assert.Null(bilhetes.Emitir("jwt"));

        relogio.Agora += BilhetesHub.Validade;
        var novo = bilhetes.Emitir("jwt-novo");

        Assert.NotNull(novo);
        Assert.Equal("jwt-novo", bilhetes.Consumir(novo.Value.Bilhete));
    }

    /// <summary>Dois bilhetes seguidos são diferentes, e cada um devolve o JWT com que foi emitido.</summary>
    [Fact]
    public void I112_123_Bilhetes_SaoDiferentesEDevolvemOSeuJwt()
    {
        var bilhetes = new BilhetesHub(new RelogioDeTeste());

        var primeiro = bilhetes.Emitir("jwt-a")!.Value;
        var segundo = bilhetes.Emitir("jwt-b")!.Value;

        Assert.NotEqual(primeiro.Bilhete, segundo.Bilhete);
        Assert.Equal("jwt-b", bilhetes.Consumir(segundo.Bilhete));
        Assert.Equal("jwt-a", bilhetes.Consumir(primeiro.Bilhete));
        Assert.Null(bilhetes.Consumir(primeiro.Bilhete));
    }
}
