using CampusCarpool.Application.Identidade;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Domínios académicos configuráveis (P-01, RF-F001-03, DEF-BE-002).</summary>
public sealed class ValidadorEmailAcademicoTests
{
    private static ValidadorEmailAcademico Criar(params string[] dominios)
    {
        var opcoes = new OpcoesEmailAcademico();

        foreach (var dominio in dominios)
            opcoes.Dominios.Add(dominio);

        return new ValidadorEmailAcademico(Options.Create(opcoes));
    }

    [Theory]
    [InlineData("aluno@estg.ipp.pt", true)]
    [InlineData("ALUNO@ESTG.IPP.PT", true)]
    [InlineData("aluno@ess.ipp.pt", true)]
    [InlineData("aluno@gmail.com", false)]
    [InlineData("aluno@xestg.ipp.pt", false)]
    public void I110_60_SoAceitaOsDominiosDaLista(string email, bool esperado)
    {
        Assert.Equal(esperado, Criar("estg.ipp.pt", "ess.ipp.pt").Permitido(email));
    }

    [Fact]
    public void I110_61_DominioComArroba_TemOMesmoEfeito()
    {
        Assert.True(Criar("@estg.ipp.pt").Permitido("aluno@estg.ipp.pt"));
    }

    [Fact]
    public void I110_62_SemDominiosConfigurados_RecusaTudo()
    {
        Assert.False(Criar().Permitido("aluno@estg.ipp.pt"));
    }
}
