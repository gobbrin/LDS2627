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
    /// As respostas e os pedidos da Api são DTOs, nunca entidades nem valores do Domain. Desde a I112 (BE-06) já não
    /// há exceções: <c>Coordenadas</c> também tem DTO próprio.
    /// </summary>
    [Fact]
    public void Api_NaoUsaEntidades()
    {
        var proibidas = TiposReferenciadosPelaApi()
            .Where(t => t.StartsWith("CampusCarpool.Domain.Entities", StringComparison.Ordinal));

        Assert.Empty(proibidas);
    }

    /// <summary>
    /// I112 (BE-06): nenhuma resposta leva, a qualquer profundidade, um tipo do Domain ou das abstrações da
    /// Application. Percorre os serviços que os controllers recebem, o que cada método assíncrono devolve e,
    /// recursivamente, as propriedades desses tipos. Assim, acrescentar um campo do Domain a um DTO faz falhar o teste.
    /// </summary>
    [Fact]
    public void Respostas_NaoLevamTiposDoDomain()
    {
        var visitados = new HashSet<Type>();
        var proibidos = new List<string>();

        foreach (Type servico in ServicosUsadosPelosControllers())
        {
            foreach (MethodInfo metodo in servico.GetMethods())
            {
                // Só os métodos assíncronos devolvem respostas; os síncronos são de uso interno.
                if (typeof(Task).IsAssignableFrom(metodo.ReturnType))
                    PercorrerResposta(metodo.ReturnType, $"{servico.Name}.{metodo.Name}", visitados, proibidos);
            }
        }

        Assert.True(proibidos.Count == 0, string.Join(Environment.NewLine, proibidos));
    }

    /// <summary>
    /// I112 (BE-07): os repositórios devolvem entidades ou projeções próprias (em <c>Abstractions.Persistence</c>),
    /// nunca os DTOs de resposta dos módulos.
    /// </summary>
    [Fact]
    public void Repositorios_NaoDevolvemDtosDeResposta()
    {
        var proibidos = new List<string>();

        IEnumerable<Type> repositorios = Application.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "CampusCarpool.Application.Abstractions.Persistence");

        foreach (Type repositorio in repositorios)
        {
            foreach (MethodInfo metodo in repositorio.GetMethods())
            {
                foreach (Type tipo in TiposDentro(metodo.ReturnType))
                {
                    if (DeUmModuloDaApplication(tipo))
                        proibidos.Add($"{repositorio.Name}.{metodo.Name} devolve {tipo.Name}");
                }
            }
        }

        Assert.True(proibidos.Count == 0, string.Join(Environment.NewLine, proibidos));
    }

    // Interfaces da Application que os construtores dos controllers recebem.
    private static List<Type> ServicosUsadosPelosControllers()
    {
        var servicos = new List<Type>();

        foreach (Type controller in Api.GetTypes().Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal)))
        {
            foreach (ConstructorInfo construtor in controller.GetConstructors())
            {
                foreach (ParameterInfo parametro in construtor.GetParameters())
                {
                    if (parametro.ParameterType.IsInterface && parametro.ParameterType.Assembly == Application)
                        servicos.Add(parametro.ParameterType);
                }
            }
        }

        return servicos;
    }

    // Visita um tipo da resposta: regista-o se for proibido; senão, visita os argumentos genéricos e as propriedades.
    private static void PercorrerResposta(Type tipo, string caminho, HashSet<Type> visitados, List<string> proibidos)
    {
        foreach (Type interior in TiposDentro(tipo))
        {
            if (!visitados.Add(interior))
                continue;

            string nomeCompleto = interior.FullName ?? interior.Name;

            if (nomeCompleto.StartsWith("CampusCarpool.Domain", StringComparison.Ordinal)
                || nomeCompleto.StartsWith("CampusCarpool.Application.Abstractions", StringComparison.Ordinal))
            {
                proibidos.Add($"{caminho} leva {nomeCompleto}");
                continue;
            }

            // Os tipos do .NET (string, Guid, DateTime...) não se percorrem.
            if (!nomeCompleto.StartsWith("CampusCarpool.", StringComparison.Ordinal))
                continue;

            foreach (PropertyInfo propriedade in interior.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                PercorrerResposta(propriedade.PropertyType, $"{caminho} > {interior.Name}.{propriedade.Name}", visitados, proibidos);
        }
    }

    // Tira os invólucros (Task<>, Resultado<>, listas, arrays, Nullable<>): devolve o próprio tipo e os de dentro.
    private static List<Type> TiposDentro(Type tipo)
    {
        var tipos = new List<Type>();

        if (tipo.IsArray)
        {
            tipos.AddRange(TiposDentro(tipo.GetElementType()!));
            return tipos;
        }

        if (tipo.IsGenericType)
        {
            // O tipo genérico do projeto (por exemplo Resultado<T>) também conta, além dos argumentos.
            if (tipo.Namespace?.StartsWith("CampusCarpool.", StringComparison.Ordinal) == true)
                tipos.Add(tipo);

            foreach (Type argumento in tipo.GetGenericArguments())
                tipos.AddRange(TiposDentro(argumento));

            return tipos;
        }

        tipos.Add(tipo);
        return tipos;
    }

    // Tipo de um módulo da Application (Viagens, Rotas, ...), ou seja, fora de Abstractions e de Comum.
    private static bool DeUmModuloDaApplication(Type tipo)
    {
        string espaco = tipo.Namespace ?? "";

        return espaco.StartsWith("CampusCarpool.Application.", StringComparison.Ordinal)
            && !espaco.StartsWith("CampusCarpool.Application.Abstractions", StringComparison.Ordinal)
            && !espaco.StartsWith("CampusCarpool.Application.Comum", StringComparison.Ordinal);
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
