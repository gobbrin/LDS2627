using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.DependencyInjection;
using CampusCarpool.Infrastructure.Persistence;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>
/// Regras de dependência da arquitetura (documentação de arquitetura v03 §4.2), verificadas por reflexão para que
/// uma violação falhe nos testes em vez de ficar à espera de uma revisão.
/// </summary>
public class ArquiteturaTests
{
    private static readonly Assembly Domain = typeof(Viagem).Assembly;
    private static readonly Assembly Application = typeof(IUnidadeDeTrabalho).Assembly;
    private static readonly Assembly Infrastructure = typeof(CampusCarpoolDbContext).Assembly;
    private static readonly Assembly Api = typeof(ViagensController).Assembly;

    private static string[] Referencias(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();

    [Fact]
    public void Domain_NaoDependeDeOutrasCamadasNemDeFrameworks()
    {
        var proibidas = Referencias(Domain)
            .Where(r => r.StartsWith("CampusCarpool.", StringComparison.Ordinal)
                || r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Empty(proibidas);
    }

    [Fact]
    public void Application_DependeSoDoDomainDentroDaSolucao()
    {
        var internas = Referencias(Application).Where(r => r.StartsWith("CampusCarpool.", StringComparison.Ordinal));

        Assert.Equal(["CampusCarpool.Domain"], internas);
    }

    [Fact]
    public void Application_NaoConheceEntityFrameworkNemAspNetCore()
    {
        var proibidas = Referencias(Application)
            .Where(r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Empty(proibidas);
    }

    [Fact]
    public void Infrastructure_NaoDependeDaApi()
    {
        Assert.DoesNotContain("CampusCarpool.Api", Referencias(Infrastructure));
    }

    /// <summary>
    /// A Api só fala com a Application: não usa o <c>DbContext</c>, os repositórios (nem as interfaces) nem o EF Core.
    /// </summary>
    /// <remarks>
    /// Lê as referências a tipos nos metadados do assembly, que incluem o que a reflexão não vê, como o corpo dos
    /// métodos ou um <c>GetRequiredService</c>.
    /// </remarks>
    [Fact]
    public void Api_NaoUsaOAcessoADados()
    {
        var proibidas = TiposReferenciadosPelaApi()
            .Where(t => t.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || t.StartsWith("CampusCarpool.Application.Abstractions.Persistence", StringComparison.Ordinal));

        Assert.Empty(proibidas);
    }

    /// <summary>Da Infrastructure, a Api conhece só o ponto de registo, usado no arranque.</summary>
    [Fact]
    public void Api_SoConheceARegistoDaInfrastructure()
    {
        var proibidas = TiposReferenciadosPelaApi()
            .Where(t => t.StartsWith("CampusCarpool.Infrastructure", StringComparison.Ordinal)
                && t != typeof(InfrastructureServiceRegistration).FullName);

        Assert.Empty(proibidas);
    }

    /// <summary>
    /// As respostas e os pedidos da Api são DTOs, nunca entidades. <see cref="Coordenadas"/> é a exceção: é um valor,
    /// sem identidade nem estado.
    /// </summary>
    [Fact]
    public void Api_NaoUsaEntidades()
    {
        var proibidas = TiposReferenciadosPelaApi()
            .Where(t => t.StartsWith("CampusCarpool.Domain.Entities", StringComparison.Ordinal)
                && t != typeof(Coordenadas).FullName);

        Assert.Empty(proibidas);
    }

    private static string[] TiposReferenciadosPelaApi()
    {
        using var ficheiro = File.OpenRead(Api.Location);
        using var pe = new PEReader(ficheiro);
        var metadados = pe.GetMetadataReader();

        return metadados.TypeReferences
            .Select(metadados.GetTypeReference)
            .Select(t => $"{metadados.GetString(t.Namespace)}.{metadados.GetString(t.Name)}")
            .ToArray();
    }
}
