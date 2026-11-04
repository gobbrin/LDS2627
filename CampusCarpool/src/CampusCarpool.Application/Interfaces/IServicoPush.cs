namespace CampusCarpool.Application.Interfaces;

/// <summary>Envio de notificações push Android (Serviço de Notificações, RF-F006-11).</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Integrations/ServicoPush.cs</c> (Firebase Cloud Messaging). Não é chamado
/// diretamente pelos serviços: passa pela <see cref="IFilaNotificacoesPush"/> e pelo <c>TrabalhadorNotificacoesPush</c>.
/// </remarks>
public interface IServicoPush
{
    /// <summary>Envia o push para todos os dispositivos ativos do utilizador.</summary>
    /// <param name="utilizadorId">Destinatário.</param>
    /// <param name="titulo">Título do push.</param>
    /// <param name="mensagem">Texto do push.</param>
    /// <param name="tipo">Nome do <c>TipoNotificacao</c>, enviado nos dados.</param>
    /// <param name="ct">Cancelamento.</param>
    Task EnviarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct);
}
