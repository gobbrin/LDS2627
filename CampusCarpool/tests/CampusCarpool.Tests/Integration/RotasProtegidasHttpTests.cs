using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>RNF-SEG-09: todas as rotas exigem token, exceto as que o requisito enumera (API-10).</summary>
/// <remarks>
/// Lê as rotas da aplicação em execução, por isso um endpoint novo fica coberto sem mexer aqui. O <c>Program.cs</c> não
/// tem <c>FallbackPolicy</c>: esquecer o <c>[Authorize]</c> num controller deixaria a rota aberta, e estes testes falham.
/// </remarks>
public sealed partial class RotasProtegidasHttpTests : IDisposable
{
    /// <summary>
    /// As exceções de RNF-SEG-09: registo, confirmação de conta, reenvio, início de sessão e estado do serviço. A raiz
    /// <c>/</c> só diz que a API está a correr, por isso conta como estado do serviço.
    /// </summary>
    private static readonly string[] RotasAnonimas =
    [
        "*  /health",
        "GET /",
        "POST /api/v1/autenticacao/confirmar",
        "POST /api/v1/autenticacao/login",
        "POST /api/v1/autenticacao/reenviar-confirmacao",
        "POST /api/v1/autenticacao/registo"
    ];

    private readonly AplicacaoReal _app = new();

    public void Dispose() => _app.Dispose();

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex ParametroDeRota();

    private IEnumerable<RouteEndpoint> Rotas() =>
        _app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

    private static string Caminho(RouteEndpoint rota) =>
        "/" + ParametroDeRota().Replace(rota.RoutePattern.RawText!, Guid.Empty.ToString()).TrimStart('/');

    private static bool Anonima(RouteEndpoint rota) =>
        rota.Metadata.GetMetadata<IAllowAnonymous>() is not null
        || rota.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0;

    private static IEnumerable<string> Metodos(RouteEndpoint rota) =>
        rota.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Where(metodo => metodo != "CONNECT") ?? ["*"];

    private IEnumerable<(string Metodo, string Caminho)> RotasProtegidas() =>
        Rotas().Where(rota => !Anonima(rota)).SelectMany(rota => Metodos(rota).Select(metodo => (metodo, Caminho(rota))));

    [Fact]
    public void I110_85_RotasSemToken_SaoAsDoRequisito()
    {
        var anonimas = Rotas()
            .Where(Anonima)
            .SelectMany(rota => Metodos(rota).Select(metodo => $"{metodo,-2} {Caminho(rota)}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(RotasAnonimas.Order(StringComparer.Ordinal), anonimas);
    }

    /// <summary>Critério de RNF-SEG-09, pedido a pedido: sem token, todos os endpoints protegidos devolvem 401.</summary>
    [Fact]
    public async Task I110_86_SemToken_TodasAsRotasProtegidasDevolvem401()
    {
        using var cliente = _app.CreateClient();
        var rotas = RotasProtegidas().ToList();
        Assert.True(rotas.Count >= 40, $"Só foram encontradas {rotas.Count} rotas protegidas.");

        foreach (var (metodo, caminho) in rotas)
        {
            using var pedido = new HttpRequestMessage(new HttpMethod(metodo), caminho);
            using var resposta = await cliente.SendAsync(pedido);

            Assert.True(resposta.StatusCode == HttpStatusCode.Unauthorized, $"{metodo} {caminho}: {resposta.StatusCode}");
        }
    }

    /// <summary>Critério de RNF-SEG-09: com token inválido, todos os endpoints protegidos devolvem 401.</summary>
    [Fact]
    public async Task I110_87_ComTokenInvalido_TodasAsRotasProtegidasDevolvem401()
    {
        using var cliente = _app.CreateClient();
        var forjado = AplicacaoReal.TokenManual(Guid.NewGuid(), DateTime.UtcNow.AddHours(1), chave: new string('x', 48));
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forjado);

        foreach (var (metodo, caminho) in RotasProtegidas().ToList())
        {
            using var pedido = new HttpRequestMessage(new HttpMethod(metodo), caminho);
            using var resposta = await cliente.SendAsync(pedido);

            Assert.True(resposta.StatusCode == HttpStatusCode.Unauthorized, $"{metodo} {caminho}: {resposta.StatusCode}");
        }
    }
}
