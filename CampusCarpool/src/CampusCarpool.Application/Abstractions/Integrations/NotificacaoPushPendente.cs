namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Notificação push à espera de envio na <c>IFilaNotificacoesPush</c>.</summary>
/// <param name="UtilizadorId">Destinatário.</param>
/// <param name="Titulo">Título do push.</param>
/// <param name="Mensagem">Texto do push.</param>
/// <param name="Tipo">Nome do <c>TipoNotificacao</c>.</param>
public sealed record NotificacaoPushPendente(Guid UtilizadorId, string Titulo, string Mensagem, string Tipo);
