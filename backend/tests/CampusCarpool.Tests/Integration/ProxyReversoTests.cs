using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Limites por cliente atrás de um proxy reverso (BE-20, I112): os cabeçalhos <c>X-Forwarded-For</c> só contam quando
/// o proxy está na configuração <c>ProxyReverso</c>.
/// </summary>
public sealed class ProxyReversoTests
{
    /// <summary>Configuração em memória com os valores indicados.</summary>
    private static IConfiguration Configuracao(params (string Chave, string Valor)[] valores)
    {
        var dados = new Dictionary<string, string?>();
        foreach ((string chave, string valor) in valores)
            dados[chave] = valor;

        return new ConfigurationBuilder().AddInMemoryCollection(dados).Build();
    }

    [Fact]
    public void SemConfiguracao_NaoConfiaEmNenhumProxy()
    {
        Assert.False(ProxyReverso.Configurado(Configuracao()));
    }

    [Fact]
    public void ProxiesERedes_SaoOsUnicosConhecidos()
    {
        IConfiguration configuracao = Configuracao(
            ("ProxyReverso:Proxies:0", "10.1.2.3"),
            ("ProxyReverso:Redes:0", "172.16.0.0/12"));

        ServiceProvider servicos = new ServiceCollection().AddProxyReverso(configuracao).BuildServiceProvider();
        ForwardedHeadersOptions opcoes = servicos.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.True(ProxyReverso.Configurado(configuracao));
        // O localhost, em que o .NET confia por omissão, deixa de estar na lista.
        Assert.Equal([IPAddress.Parse("10.1.2.3")], opcoes.KnownProxies);
        Assert.Equal([System.Net.IPNetwork.Parse("172.16.0.0/12")], opcoes.KnownIPNetworks);
        Assert.Equal(1, opcoes.ForwardLimit);
    }

    [Fact]
    public void EnderecoInvalido_ImpedeOArranque()
    {
        IConfiguration configuracao = Configuracao(("ProxyReverso:Proxies:0", "nao-e-um-ip"));

        var erro = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddProxyReverso(configuracao));

        Assert.Equal("ProxyReverso:Proxies tem um endereço IP inválido: 'nao-e-um-ip'.", erro.Message);
    }

    /// <summary>
    /// Envia 11 logins em simultâneo, cada um com um IP de cliente diferente no <c>X-Forwarded-For</c>, e devolve os
    /// estados das respostas.
    /// </summary>
    /// <remarks>
    /// Em simultâneo, para os 11 caberem na mesma janela de um minuto do limitador: o limite é verificado à entrada do
    /// pedido, antes do login. Em série, com a máquina carregada, os 11 logins podiam passar de um minuto (R1).
    /// </remarks>
    private static async Task<HttpStatusCode[]> OnzeLoginsDeClientesDiferentesAsync(HttpClient cliente)
    {
        return await Task.WhenAll(Enumerable.Range(1, 11).Select(i => LoginDoClienteAsync(cliente, $"198.51.100.{i}")));
    }

    /// <summary>Faz um login errado em nome do cliente <paramref name="ipDoCliente"/> e devolve o estado da resposta.</summary>
    private static async Task<HttpStatusCode> LoginDoClienteAsync(HttpClient cliente, string ipDoCliente)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "/api/v1/autenticacao/login")
        {
            Content = JsonContent.Create(new { email = "ninguem@estg.ipp.pt", password = "Palavra-errada-1" })
        };
        pedido.Headers.Add("X-Forwarded-For", ipDoCliente);

        using var resposta = await cliente.SendAsync(pedido);
        return resposta.StatusCode;
    }

    /// <summary>
    /// Põe o IP de ligação de todos os pedidos em <paramref name="ipDoProxy"/>, antes do pipeline da API: o servidor de
    /// testes não tem IP de ligação, e sem IP o <c>UseForwardedHeaders</c> não consegue verificar o proxy.
    /// </summary>
    private sealed class IpDeLigacao(string ipDoProxy) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((contexto, seguinte) =>
                {
                    contexto.Connection.RemoteIpAddress = IPAddress.Parse(ipDoProxy);
                    return seguinte();
                });
                next(app);
            };
        }
    }

    /// <summary>Aplicação real atrás do proxy <c>10.1.2.3</c>, com <paramref name="proxyConfigurado"/> na configuração.</summary>
    private static WebApplicationFactory<Program> AtrasDoProxy(AplicacaoReal app, string? proxyConfigurado)
    {
        return app.WithWebHostBuilder(builder =>
        {
            if (proxyConfigurado is not null)
                builder.UseSetting("ProxyReverso:Proxies:0", proxyConfigurado);

            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new IpDeLigacao("10.1.2.3")));
        });
    }

    /// <summary>Com o proxy configurado, 11 clientes atrás dele têm cada um o seu limite de 10 logins: nenhum 429.</summary>
    [Fact]
    public async Task ProxyConfigurado_CadaClienteTemOSeuLimite()
    {
        using var app = new AplicacaoReal();
        using var comProxy = AtrasDoProxy(app, "10.1.2.3");
        using var cliente = comProxy.CreateClient();

        HttpStatusCode[] estados = await OnzeLoginsDeClientesDiferentesAsync(cliente);

        Assert.DoesNotContain((HttpStatusCode)429, estados);
    }

    /// <summary>
    /// Um pedido que não vem do proxy configurado não é acreditado: o cabeçalho é ignorado, os 11 pedidos partilham o
    /// limite do IP de ligação: 10 passam e um dá 429.
    /// </summary>
    [Theory]
    [InlineData("10.9.9.9")]
    [InlineData(null)]
    public async Task ProxyDesconhecidoOuSemConfiguracao_CabecalhoIgnorado(string? proxyConfigurado)
    {
        using var app = new AplicacaoReal();
        using var semConfianca = AtrasDoProxy(app, proxyConfigurado);
        using var cliente = semConfianca.CreateClient();

        HttpStatusCode[] estados = await OnzeLoginsDeClientesDiferentesAsync(cliente);

        // Em simultâneo não se sabe qual dos 11 chega em último; conta-se quantos foram recusados.
        Assert.Equal(1, estados.Count(estado => estado == (HttpStatusCode)429));
        Assert.Equal(10, estados.Count(estado => estado == HttpStatusCode.Unauthorized));
    }
}
