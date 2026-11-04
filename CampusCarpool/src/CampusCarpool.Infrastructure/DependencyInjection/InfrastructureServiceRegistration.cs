using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Infrastructure.DependencyInjection;

/// <summary>Registo da camada Infrastructure: base de dados, repositórios, serviços e trabalhadores em segundo plano.</summary>
/// <remarks>
/// As integrações externas com <c>HttpClient</c> (<c>ServicoEmail</c>, <c>ServicoPush</c>) e o
/// <c>HistoricoService</c> são registados no <c>Program.cs</c> da Api, não aqui.
/// </remarks>
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

        // Repositório de notificações e dispositivos.
        services.AddScoped<IRepositorioNotificacoes, RepositorioNotificacoes>();
        services.AddScoped<IRepositorioDispositivos, RepositorioDispositivo>();


        // Repositório de participações.
        services.AddScoped<IRepositorioParticipacoes, RepositorioParticipacoes>();

        // Repositório de viagens.
        services.AddScoped<IRepositorioViagens, RepositorioViagens>();



        // Serviço de avisos do condutor (I044).
        services.AddScoped<IAvisoService, AvisoService>();

        // Serviço da unidade de trabalho.
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoEfCore>();

        // Serviço da unidade de trabalho.
        // Registo repetido (T6, fica para m4): é inofensivo, o segundo substitui o primeiro.
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoEfCore>();

        // I048 - Repositório de avaliações.
        services.AddScoped<IRepositorioAvaliacoes, RepositorioAvaliacoes>();

        // Fila partilhada de notificações push (I047).
        // Singleton: a mesma fila em memória é partilhada por todos os pedidos e pelo trabalhador.
        services.AddSingleton<IFilaNotificacoesPush, FilaNotificacoesPush>();

        // Trabalhador que processa a fila em segundo plano (I047).
        services.AddHostedService<TrabalhadorNotificacoesPush>();

        // Conclusão automática das viagens (I110, DEF-BE-009; UC-F002-05).
        services.AddScoped<IConclusaoViagensService, ConclusaoViagensService>();
        services.AddHostedService<TrabalhadorConclusaoViagens>();

        return services;
    }
}



