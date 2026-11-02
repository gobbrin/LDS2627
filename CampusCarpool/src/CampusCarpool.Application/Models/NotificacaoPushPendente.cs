namespace CampusCarpool.Application.Models;

public sealed record NotificacaoPushPendente(Guid UtilizadorId, string Titulo, string Mensagem, string Tipo);
