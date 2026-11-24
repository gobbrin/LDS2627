namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Envio de notificações push Android (Serviço de Notificações, RF-F006-11).</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Integrations/ServicoPush.cs</c> (Firebase Cloud Messaging). Não é chamado
/// diretamente pelos serviços: passa pela <see cref="IFilaNotificacoesPush"/> e pelo <c>TrabalhadorNotificacoesPush</c>.
/// </remarks>
public interface IServicoPush
{
    /// <summary>Envia o push para todos os dispositivos ativos do destinatário.</summary>
    /// <param name="notificacao">Destinatário, texto e dados (<c>tipo</c>, <c>notificacaoId</c>, <c>viagemId</c>) do push.</param>
    /// <param name="ct">Cancelamento.</param>
    Task EnviarAsync(NotificacaoPushPendente notificacao, CancellationToken ct);
}
