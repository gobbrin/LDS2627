using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Application.DependencyInjection;
public static class ApplicationServiceRegistration {

    //Regista os serviços da camada Application.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) {
        // Gere a lógica de negócio das mensagens, notificações e participações.
        services.AddScoped<IMensagemService, MensagemService>();
        services.AddScoped<INotificacaoService, NotificacaoService>();
        services.AddScoped<IParticipacaoService, ParticipacaoService>();
        
        return services;
    }
}
