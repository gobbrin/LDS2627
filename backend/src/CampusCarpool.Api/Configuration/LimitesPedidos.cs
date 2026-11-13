using System.Security.Claims;
using System.Threading.RateLimiting;
using CampusCarpool.Api.Controllers;

namespace CampusCarpool.Api.Configuration;

/// <summary>Limites de pedidos da API (G19, A6): janela fixa de um minuto, sem fila, 429 acima do limite.</summary>
/// <remarks>
/// Limite global de 100 pedidos por minuto, por utilizador com sessão ou por IP sem sessão, que vale também para o
/// hub e para <c>/health</c>. Por isso o <c>UseRateLimiter</c> vem depois do <c>UseAuthentication</c>. A política
/// <see cref="Autenticacao"/> acrescenta 10 pedidos por minuto por IP no login, no registo e no reenvio da
/// confirmação, contra tentativas de adivinhar palavras-passe; não há bloqueio de conta (P-18).
/// </remarks>
public static class LimitesPedidos
{
    /// <summary>Nome da política dos endpoints sem sessão que validam credenciais ou enviam emails.</summary>
    public const string Autenticacao = "autenticacao";

    private const int PedidosPorMinuto = 100;
    private const int PedidosAutenticacaoPorMinuto = 10;

    /// <summary>Regista o limite global e a política <see cref="Autenticacao"/>.</summary>
    public static IServiceCollection AddLimitesPedidos(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (contexto, ct) => new ValueTask(contexto.HttpContext.Response.WriteAsJsonAsync(
                new CorpoErro("Demasiados pedidos. Aguarda um minuto e tenta novamente."), ct));
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                Limite(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Ip(context), PedidosPorMinuto));
            options.AddPolicy(Autenticacao, context => Limite(Ip(context), PedidosAutenticacaoPorMinuto));
        });

    private static string Ip(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static RateLimitPartition<string> Limite(string chave, int pedidos) =>
        RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = pedidos,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
}
