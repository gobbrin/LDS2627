using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Infrastructure.DependencyInjection;
using CampusCarpool.Infrastructure.Integrations;
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

    [Fact]
    /// <summary>
    /// D-5: a aplicação usa o serviço com reserva (HERE e OpenRouteService); o cliente da HERE tem 5 s, para sobrar
    /// tempo para a reserva dentro do limite de 10 s da operação, e o do OpenRouteService mantém os 10 s.
    /// </summary>
    public void I112_79_MapasComReserva_ClienteDaHereCom5Segundos()
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
        IHttpClientFactory fabrica = fornecedor.GetRequiredService<IHttpClientFactory>();
        using var clienteHere = fabrica.CreateClient(nameof(ServicoMapasHere));
        using var clienteReserva = fabrica.CreateClient(nameof(ServicoMapasRotas));

        Assert.IsType<ServicoMapasComReserva>(fornecedor.GetRequiredService<IServicoMapasRotas>());
        Assert.Equal(TimeSpan.FromSeconds(5), clienteHere.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(10), clienteReserva.Timeout);
    }
}
