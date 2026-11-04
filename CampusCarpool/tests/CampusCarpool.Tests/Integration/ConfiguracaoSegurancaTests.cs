using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — API-01/DEF-BE-026, C3/DC-18, C4: o appsettings.json versionado
// não pode ter segredos e a API não pode arrancar sem uma Jwt:Key própria.
public sealed class ConfiguracaoSegurancaTests
{
    private sealed class ApiSemChaveFactory(string? chave) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=(local);Database=CampusCarpool_Tests;");

            // Sobrepõe-se a User Secrets ou variáveis de ambiente da máquina.
            builder.UseSetting("Jwt:Key", chave ?? string.Empty);
        }
    }

    private static IConfigurationRoot LerAppSettingsVersionado()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);

        while (pasta is not null)
        {
            var caminho = Path.Combine(
                pasta.FullName, "src", "CampusCarpool.Api", "appsettings.json");

            if (File.Exists(caminho))
            {
                return new ConfigurationBuilder()
                    .AddJsonFile(caminho, optional: false, reloadOnChange: false)
                    .Build();
            }

            pasta = pasta.Parent;
        }

        throw new FileNotFoundException(
            "src/CampusCarpool.Api/appsettings.json não encontrado.");
    }

    [Fact]
    public void I110_01_AppSettingsVersionado_NaoTemJwtKeyNemMapsApiKey()
    {
        var config = LerAppSettingsVersionado();

        Assert.True(string.IsNullOrEmpty(config["Jwt:Key"]));
        Assert.True(string.IsNullOrEmpty(config["Maps:ApiKey"]));
    }

    [Fact]
    public void I110_02_AppSettingsVersionado_ConfirmUrlApontaParaConfirmarConta()
    {
        var config = LerAppSettingsVersionado();

        Assert.EndsWith("/confirmar-conta", config["Frontend:ConfirmUrl"]);
    }

    [Fact]
    public void I110_03_AppSettingsVersionado_NaoTemSecaoEmailMorta()
    {
        var config = LerAppSettingsVersionado();

        Assert.False(config.GetSection("Email").Exists());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("chave-curta")]
    public void I110_04_ArranqueSemJwtKeyValida_Recusado(string? chave)
    {
        using var factory = new ApiSemChaveFactory(chave);

        var erro = Assert.Throws<InvalidOperationException>(
            () => factory.CreateClient());

        Assert.Contains("Jwt:Key", erro.Message);
    }
}
