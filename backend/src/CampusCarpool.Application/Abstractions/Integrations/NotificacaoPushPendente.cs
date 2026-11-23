namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Notificação push à espera de envio na <c>IFilaNotificacoesPush</c>.</summary>
/// <param name="NotificacaoId">Id da notificação gravada, para a aplicação a marcar como lida a partir do push (BE-17).</param>
/// <param name="UtilizadorId">Destinatário.</param>
/// <param name="ViagemId">Viagem a que o push se refere, para a aplicação abrir o ecrã certo; <c>null</c> se não houver.</param>
/// <param name="Titulo">Título do push.</param>
/// <param name="Mensagem">Texto do push.</param>
/// <param name="Tipo">Nome do <c>TipoNotificacao</c>.</param>
public sealed record NotificacaoPushPendente(
    Guid NotificacaoId,
    Guid UtilizadorId,
    Guid? ViagemId,
    string Titulo,
    string Mensagem,
    string Tipo);
