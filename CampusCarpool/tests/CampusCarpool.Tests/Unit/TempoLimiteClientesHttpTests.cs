using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Os clientes HTTP chamados durante um pedido não usam os 100 s por omissão.</summary>
public sealed class TempoLimiteClientesHttpTests
{
    [Fact]
    public void I110_153_ClienteDoEmail_TemTempoLimiteCurto()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=CampusCarpool_Tests;"
            })
            .Build();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddSingleton<IConfiguration>(configuracao);
        servicos.AddInfrastructureServices(configuracao);

        using var fornecedor = servicos.BuildServiceProvider();
        using var cliente = fornecedor.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IServicoEmail));

        Assert.Equal(TimeSpan.FromSeconds(10), cliente.Timeout);
    }
}
