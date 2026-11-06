using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Application.Historico;
using CampusCarpool.Application.Identidade;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCarpool.Application.DependencyInjection;

/// <summary>Registo dos serviços da camada Application no contentor de injeção de dependências.</summary>
/// <remarks>
/// <see cref="OpcoesCustos"/> e <see cref="OpcoesEmailAcademico"/> são ligados no <c>Program.cs</c>.
/// </remarks>
public static class ApplicationServiceRegistration
{

    /// <summary>Regista os serviços da camada Application, todos com tempo de vida <c>Scoped</c> (um por pedido HTTP).</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A mesma coleção, para encadear chamadas.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Gere a lógica de negócio das mensagens, notificações e participações.
        services.AddScoped<IAvisoService, AvisoService>();
        services.AddScoped<IHistoricoService, HistoricoService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        services.AddScoped<IDispositivoService, DispositivoService>();
        services.AddSingleton<ValidadorEmailAcademico>();
        services.AddScoped<IMensagemService, MensagemService>();
        services.AddScoped<INotificacaoService, NotificacaoService>();
        services.AddScoped<IParticipacaoService, ParticipacaoService>();
        services.AddScoped<IRotaService, RotaService>();
        services.AddScoped<IConsultaRotasService, ConsultaRotasService>();
        services.AddScoped<EstimativaCustoService>();
        services.AddScoped<IConclusaoViagensService, ConclusaoViagensService>();
        services.AddScoped<IConsultaViagensService, ConsultaViagensService>();
        services.AddScoped<IPesquisaService, PesquisaService>();
        services.AddScoped<IViagemService, ViagemService>();

        // I048 - Avaliações
        services.AddScoped<IAvaliacaoService, AvaliacaoService>();

        return services;
    }
}
