using CampusCarpool.Application.Identidade;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>O validador recusa endereços malformados que terminam num domínio académico (RF-F001-03).</summary>
public sealed class ValidadorEmailFormatoTests
{
    private static ValidadorEmailAcademico Validador()
    {
        var opcoes = new OpcoesEmailAcademico();
        opcoes.Dominios.Add("estg.ipp.pt");
        return new ValidadorEmailAcademico(Options.Create(opcoes));
    }

    [Theory]
    [InlineData("@estg.ipp.pt")]
    [InlineData("a b@estg.ipp.pt")]
    [InlineData(" aluno@estg.ipp.pt")]
    [InlineData("x@gmail.com@estg.ipp.pt")]
    [InlineData("aluno@gmail.com")]
    public void Recusa(string email) => Assert.False(Validador().Permitido(email));

    [Theory]
    [InlineData("aluno@estg.ipp.pt")]
    [InlineData("nome.apelido+teste@estg.ipp.pt")]
    [InlineData("8220000@ESTG.IPP.PT")]
    public void Aceita(string email) => Assert.True(Validador().Permitido(email));
}
