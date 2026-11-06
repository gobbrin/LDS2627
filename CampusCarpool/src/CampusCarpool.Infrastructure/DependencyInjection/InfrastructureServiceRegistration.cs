using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Infrastructure.BackgroundJobs;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Infrastructure.DependencyInjection;

/// <summary>
/// Registo da camada Infrastructure: base de dados, repositórios, integrações externas e trabalhadores em segundo plano.
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>Regista os serviços e o acesso à base de dados.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração; tem de ter <c>ConnectionStrings:DefaultConnection</c>.</param>
    /// <returns>A mesma coleção, para encadear chamadas.</returns>
    /// <exception cref="InvalidOperationException">Falta a connection string (a API não arranca sem ela).</exception>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Obtém a ligação à base de dados configurada.
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não configurada.");

        // Configura o Entity Framework Core para SQL Server.
        // As migrations não são aplicadas ao arrancar: aplicam-se à parte (dotnet ef database update ou script).
        services.AddDbContext<CampusCarpoolDbContext>(
            options => options.UseSqlServer(connection));


        // Repositórios de mensagens e utilizadores.
        services.AddScoped<IRepositorioMensagens, RepositorioMensagens>();
        services.AddScoped<IRepositorioUtilizadores, RepositorioUtilizadores>();
        services.AddScoped<IRepositorioLigacoesExternas, RepositorioLigacoesExternas>();
        services.AddScoped<IRepositorioConfirmacoesConta, RepositorioConfirmacoesConta>();
        services.AddScoped<IRepositorioTokensRevogados, RepositorioTokensRevogados>();

        // Palavras-passe e tokens de acesso.
        services.AddSingleton<IHashPalavraPasse, HashPalavraPasse>();
        services.AddSingleton<IGeradorTokens, GeradorTokensJwt>();

        // Integrações externas com HttpClient gerido pela fábrica (Brevo, FCM, OpenRouteService).
        services.AddHttpClient<IServicoEmail, ServicoEmail>();
        services.AddHttpClient<IServicoPush, ServicoPush>();

        // O BaseAddress não tem efeito: o ServicoMapasRotas usa URLs absolutos (IN-04). O timeout (10 s) aplica-se a cada pedido.
        services.AddHttpClient<IServicoMapasRotas, ServicoMapasRotas>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(
                config["Maps:BaseUrl"] ?? "https://api.heigit.org/"
                .TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(
                config.GetValue("Maps:TimeoutSeconds", 10));
        });

        // Repositório de notificações e dispositivos.
        services.AddScoped<IRepositorioNotificacoes, RepositorioNotificacoes>();
        services.AddScoped<IRepositorioDispositivos, RepositorioDispositivo>();


        // Repositório de participações.
        services.AddScoped<IRepositorioParticipacoes, RepositorioParticipacoes>();

        // Repositório de viagens.
        services.AddScoped<IRepositorioViagens, RepositorioViagens>();



        // Consultas do histórico pessoal (I049).
        services.AddScoped<IRepositorioHistorico, RepositorioHistorico>();

        // Repositório de avisos do condutor (I044).
        services.AddScoped<IRepositorioAvisos, RepositorioAvisos>();

        // Serviço da unidade de trabalho.
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoEfCore>();

        // I048 - Repositório de avaliações.
        services.AddScoped<IRepositorioAvaliacoes, RepositorioAvaliacoes>();

        // Fila partilhada de notificações push (I047).
        // Singleton: a mesma fila em memória é partilhada por todos os pedidos e pelo trabalhador.
        services.AddSingleton<IFilaNotificacoesPush, FilaNotificacoesPush>();

        // Trabalhador que processa a fila em segundo plano (I047).
        services.AddHostedService<TrabalhadorNotificacoesPush>();

        // Conclusão automática das viagens (I110, DEF-BE-009; UC-F002-05).
        services.AddHostedService<TrabalhadorConclusaoViagens>();

        return services;
    }
}



