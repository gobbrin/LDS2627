using System.Security.Claims;

namespace CampusCarpool.Api.Controllers;

/// <summary>Leitura do utilizador autenticado a partir das claims do token.</summary>
internal static class UtilizadorAutenticado
{
    /// <summary>
    /// Id do utilizador, ou <c>null</c> se o token não o tiver. O JwtBearer mapeia a claim <c>sub</c> para
    /// <see cref="ClaimTypes.NameIdentifier"/>; a claim original fica como alternativa.
    /// </summary>
    public static Guid? IdUtilizador(this ClaimsPrincipal utilizador) =>
        Guid.TryParse(TextoDoId(utilizador), out var id) ? id : null;

    /// <summary>A claim do id, tal como vem no token.</summary>
    public static string? TextoDoId(this ClaimsPrincipal utilizador) =>
        utilizador.FindFirstValue(ClaimTypes.NameIdentifier) ?? utilizador.FindFirstValue("sub");
}
