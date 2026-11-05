using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Application.DependencyInjection;

/// <summary>Registo dos serviços da camada Application no contentor de injeção de dependências.</summary>
/// <remarks>
/// <see cref="OpcoesCustos"/> é ligado no <c>Program.cs</c>. O <c>AvisoService</c> e o <c>HistoricoService</c> estão
/// na Infrastructure (AP-04, DC-39), e o <c>HistoricoService</c> é registado no <c>Program.cs</c>.
/// </remarks>
public static class ApplicationServiceRegistration
{

    /// <summary>Regista os serviços da camada Application, todos com tempo de vida <c>Scoped</c> (um por pedido HTTP).</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A mesma coleção, para encadear chamadas.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Gere a lógica de negócio das mensagens, notificações e participações.
        services.AddScoped<IMensagemService, MensagemService>();
        services.AddScoped<INotificacaoService, NotificacaoService>();
        services.AddScoped<IParticipacaoService, ParticipacaoService>();
        services.AddScoped<IRotaService, RotaService>();
        services.AddScoped<EstimativaCustoService>();
        services.AddScoped<IConclusaoViagensService, ConclusaoViagensService>();

        // I048 - Avaliações
        services.AddScoped<IAvaliacaoService, AvaliacaoService>();

        return services;
    }
}
