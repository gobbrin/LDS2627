using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Identidade;
using CampusCarpool.Application.Rotas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Registo real do <c>Program.cs</c>: a maioria dos testes cria os controllers à mão, por isso um serviço por registar
/// só se veria em execução. Não liga à base de dados nem arranca os trabalhos em segundo plano.
/// </summary>
public sealed class RegistoServicosTests
{
    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(local);Database=CampusCarpool_Tests;");
            builder.UseSetting("Jwt:Key", "CHAVE_DE_TESTES_I110_COM_MAIS_DE_32_BYTES_12345");
            builder.UseSetting("Custos:PrecoPorKm", "0.25");

            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        }
    }

    [Fact]
    public void TodosOsControllers_SaoResolvidos()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();

        var controllers = typeof(ViagensController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        Assert.All(controllers, tipo =>
            Assert.NotNull(ActivatorUtilities.CreateInstance(scope.ServiceProvider, tipo)));
    }

    [Fact]
    public void PrecoPorKm_VemDaSeccaoCustos()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();

        var estimativa = scope.ServiceProvider.GetRequiredService<EstimativaCustoService>().Calcular(10m, 0);

        Assert.Equal(0.25m, estimativa.PrecoPorKm);
    }

    [Fact]
    public void DominiosAcademicos_VemDaSeccaoEmailAcademico()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();

        var validador = scope.ServiceProvider.GetRequiredService<ValidadorEmailAcademico>();

        Assert.True(validador.Permitido("aluno@estg.ipp.pt"));
        Assert.False(validador.Permitido("aluno@gmail.com"));
    }
}
