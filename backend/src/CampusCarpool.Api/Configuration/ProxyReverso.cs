using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace CampusCarpool.Api.Configuration;

/// <summary>
/// Cabeçalhos <c>X-Forwarded-For</c> e <c>X-Forwarded-Proto</c> de um proxy reverso (BE-20, C8, P-18): o limitador
/// passa a contar pelo IP real do cliente e não pelo IP do proxy.
/// </summary>
/// <remarks>
/// Só se confia nos proxies indicados na configuração: <c>ProxyReverso:Proxies</c> (endereços IP) e
/// <c>ProxyReverso:Redes</c> (redes em notação CIDR, por exemplo <c>10.0.0.0/8</c>). Sem nenhum, os cabeçalhos são
/// ignorados, porque qualquer cliente os pode escrever para fugir ao limite. Aceita-se um só salto (o proxy mais
/// próximo). Manual: <c>I064-MANUAL-INSTALACAO-BACKEND.md</c>.
/// </remarks>
public static class ProxyReverso
{
    /// <summary>Secção da configuração.</summary>
    public const string Seccao = "ProxyReverso";

    /// <summary>Indica se há algum proxy ou rede configurados; sem eles, a API não lê os cabeçalhos do proxy.</summary>
    /// <param name="configuration">Configuração da aplicação.</param>
    public static bool Configurado(IConfiguration configuration)
    {
        return Proxies(configuration).Length > 0 || Redes(configuration).Length > 0;
    }

    /// <summary>Regista as opções do <c>UseForwardedHeaders</c> com os proxies e as redes da configuração.</summary>
    /// <param name="services">Serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <exception cref="InvalidOperationException">Um endereço ou uma rede com formato inválido: a API não arranca.</exception>
    public static IServiceCollection AddProxyReverso(this IServiceCollection services, IConfiguration configuration)
    {
        // Lê e valida já, para um erro de configuração parar o arranque em vez de aparecer no primeiro pedido.
        List<IPAddress> proxies = LerProxies(configuration);
        List<System.Net.IPNetwork> redes = LerRedes(configuration);

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            // Por omissão o .NET confia no localhost; aqui confia-se só no que está configurado.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (IPAddress proxy in proxies)
                options.KnownProxies.Add(proxy);

            foreach (System.Net.IPNetwork rede in redes)
                options.KnownIPNetworks.Add(rede);
        });
    }

    // Converte ProxyReverso:Proxies em endereços IP.
    private static List<IPAddress> LerProxies(IConfiguration configuration)
    {
        var resultado = new List<IPAddress>();

        foreach (string texto in Proxies(configuration))
        {
            if (!IPAddress.TryParse(texto, out IPAddress? endereco))
                throw new InvalidOperationException($"{Seccao}:Proxies tem um endereço IP inválido: '{texto}'.");

            resultado.Add(endereco);
        }

        return resultado;
    }

    // Converte ProxyReverso:Redes (CIDR) em redes.
    private static List<System.Net.IPNetwork> LerRedes(IConfiguration configuration)
    {
        var resultado = new List<System.Net.IPNetwork>();

        foreach (string texto in Redes(configuration))
        {
            if (!System.Net.IPNetwork.TryParse(texto, out System.Net.IPNetwork rede))
                throw new InvalidOperationException($"{Seccao}:Redes tem uma rede inválida (usar CIDR, ex.: 10.0.0.0/8): '{texto}'.");

            resultado.Add(rede);
        }

        return resultado;
    }

    private static string[] Proxies(IConfiguration configuration) =>
        configuration.GetSection($"{Seccao}:Proxies").Get<string[]>() ?? [];

    private static string[] Redes(IConfiguration configuration) =>
        configuration.GetSection($"{Seccao}:Redes").Get<string[]>() ?? [];
}
