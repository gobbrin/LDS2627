using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.Integrations;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    // Regista os serviços e o acesso à base de dados.
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Obtém a ligação à base de dados configurada.
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não configurada.");

        // Configura o Entity Framework Core para SQL Server.
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

        // Fila partilhada de notificações push.
        services.AddSingleton<IFilaNotificacoesPush, FilaNotificacoesPush>();

        // Trabalhador que processa a fila em segundo plano.
        services.AddHostedService<TrabalhadorNotificacoesPush>();

        return services;
    }
    
}
