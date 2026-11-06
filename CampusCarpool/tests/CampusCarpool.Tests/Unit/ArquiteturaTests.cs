using System.Reflection;
using System.Runtime.CompilerServices;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
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

    /// <summary>
    /// Tipos da Api que ainda recebem o <c>DbContext</c>. A lista só pode encolher: cada controller sai daqui
    /// quando a sua lógica passar para a Application, e no fim fica vazia.
    /// </summary>
    /// <remarks>
    /// A reflexão só vê construtores, parâmetros e campos: não deteta um <c>GetRequiredService</c>, como o que o
    /// <c>ValidacaoTokensRevogados</c> ainda faz.
    /// </remarks>
    private static readonly string[] ApiAindaComDbContext =
    [
        "AutenticacaoController",
        "UtilizadoresController",
        "ViagensController",
    ];

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

    [Fact]
    public void Api_NaoUsaODbContextForaDasExcecoesConhecidas()
    {
        var comDbContext = TiposDaApiComDbContext();

        Assert.Empty(comDbContext.Except(ApiAindaComDbContext));
    }

    /// <summary>Falha quando um tipo deixa de usar o <c>DbContext</c> e continua na lista, para ela não ficar desatualizada.</summary>
    [Fact]
    public void Api_ListaDeExcecoesNaoTemEntradasObsoletas()
    {
        var comDbContext = TiposDaApiComDbContext();

        Assert.Empty(ApiAindaComDbContext.Except(comDbContext));
    }

    // Sem os tipos gerados pelo compilador: em Debug, os métodos assíncronos guardam as variáveis locais em campos
    // da máquina de estados e em Release não, e o resultado tem de ser o mesmo nas duas configurações.
    private static string[] TiposDaApiComDbContext() =>
        Api.GetTypes()
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(UsaDbContext)
            .Select(t => t.Name)
            .ToArray();

    private static bool UsaDbContext(Type tipo)
    {
        const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var parametros = tipo.GetConstructors(Todos).SelectMany(c => c.GetParameters())
            .Concat(tipo.GetMethods(Todos).SelectMany(m => m.GetParameters()));

        return parametros.Any(p => p.ParameterType == typeof(CampusCarpoolDbContext))
            || tipo.GetFields(Todos).Any(f => f.FieldType == typeof(CampusCarpoolDbContext));
    }
}
