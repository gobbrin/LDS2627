namespace CampusCarpool.Domain.Enums;

/// <summary>Tipos de notificação (F006).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>PedidoRecebido</c>: o condutor recebeu um pedido (RF-F006-13). Gera push.</item>
/// <item><c>PedidoAceite</c> / <c>PedidoRejeitado</c>: decisão do condutor (RF-F006-01). Geram push.</item>
/// <item><c>AlteracaoViagem</c>: alteração, indisponibilização, conclusão ou cancelamento (RF-F006-02 a 04, 14). Sem push (RF-F006-16).</item>
/// <item><c>Aviso</c>: aviso do condutor (RF-F006-05). Sem push (RF-F006-17).</item>
/// <item><c>NovaMensagem</c>: mensagem individual recebida (RF-F006-06). Sem push (RF-F006-12).</item>
/// </list>
/// Persistido como texto (nome do membro): mudar o nome de um membro exige uma migration de dados.
/// </remarks>
public enum TipoNotificacao { PedidoRecebido, PedidoAceite, PedidoRejeitado, AlteracaoViagem, Aviso, NovaMensagem }
