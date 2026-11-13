namespace CampusCarpool.Api.Configuration;

/// <summary>Origens que podem chamar a API a partir do browser (DC-45, RNF-COM-01).</summary>
/// <remarks>
/// A lista vem de <c>Cors:Origens</c>; vazia, nenhum browser noutra origem chama a API. As credenciais têm de ficar
/// autorizadas porque o cliente SignalR do browser as pede na negociação com o hub, e por isso não se aceita <c>*</c>.
/// O cliente Android não usa CORS.
/// </remarks>
public static class PoliticaCors
{
    private const string Seccao = "Cors:Origens";

    /// <summary>Regista a política por omissão com as origens da configuração.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção, para encadear chamadas.</returns>
    public static IServiceCollection AddPoliticaCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origens = configuration.GetSection(Seccao).Get<string[]>() ?? [];

        return services.AddCors(options => options.AddDefaultPolicy(politica =>
            politica.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
    }
}
