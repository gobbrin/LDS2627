
namespace CampusCarpool.Application.Interfaces;

/// <summary>Histórico pessoal do utilizador autenticado (F009).</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Services/HistoricoService.cs</c> e registado no <c>Program.cs</c>.
/// Só o próprio utilizador consulta o seu histórico (RF-F009-07). Devolve tudo de uma vez, sem paginação (AP-10).
/// </remarks>
public interface IHistoricoService
{
    /// <summary>Devolve as seis listas do histórico do utilizador (RF-F009-01 a RF-F009-06).</summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct);
}

/// <summary>Viagem no histórico, como condutor ou como passageiro.</summary>
/// <param name="Id">Identificador da viagem.</param>
/// <param name="Origem">Origem da viagem.</param>
/// <param name="Destino">Destino da viagem.</param>
/// <param name="PartidaEm">Data e hora de partida.</param>
/// <param name="Estado">Estado da viagem (<c>Ativa</c>, <c>Indisponibilizada</c>, <c>Concluida</c>).</param>
/// <param name="LugaresOferecidos">Lugares oferecidos; só preenchido nas viagens como condutor.</param>
/// <param name="ParticipacaoId">Participação do utilizador; só nas viagens como passageiro.</param>
/// <param name="EstadoParticipacao">Estado dessa participação; só nas viagens como passageiro.</param>
public sealed record ViagemHistorico(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    string Estado,
    int? LugaresOferecidos,
    Guid? ParticipacaoId,
    string? EstadoParticipacao
);

/// <summary>Pedido de participação no histórico (efetuado pelo utilizador ou recebido como condutor, RF-F009-04).</summary>
/// <param name="Id">Identificador da participação.</param>
/// <param name="ViagemId">Viagem pedida.</param>
/// <param name="Origem">Origem da viagem.</param>
/// <param name="Destino">Destino da viagem.</param>
/// <param name="PartidaEm">Partida da viagem (ordenação, RF-F009-06).</param>
/// <param name="CriadoEm">Data do pedido.</param>
/// <param name="Estado">Estado do pedido (nome de <c>EstadoParticipacao</c>).</param>
public sealed record PedidoHistorico(
    Guid Id,
    Guid ViagemId,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    DateTime CriadoEm,
    string Estado
);

/// <summary>Notificação no histórico (RF-F009-05).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ViagemId">Viagem relacionada, se existir.</param>
/// <param name="Tipo">Nome do <c>TipoNotificacao</c>.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Conteudo">Texto.</param>
/// <param name="CriadaEm">Data de criação.</param>
/// <param name="LidaEm">Data de leitura, ou <c>null</c>.</param>
public sealed record NotificacaoHistorico(
    Guid Id,
    Guid? ViagemId,
    string Tipo,
    string Titulo,
    string Conteudo,
    DateTime CriadaEm,
    DateTime? LidaEm
);

/// <summary>Aviso do condutor no histórico (RF-F009-05).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ViagemId">Viagem do aviso.</param>
/// <param name="Conteudo">Texto do aviso.</param>
/// <param name="CriadoEm">Data de envio.</param>
public sealed record AvisoHistorico(
    Guid Id,
    Guid ViagemId,
    string Conteudo,
    DateTime CriadoEm
);

/// <summary>Histórico completo, devolvido por <c>GET /api/v1/historico</c>.</summary>
/// <param name="ViagensComoCondutor">
/// Viagens criadas pelo utilizador que já partiram ou já não estão ativas (RF-F009-02); as ativas futuras não entram.
/// </param>
/// <param name="ViagensComoPassageiro">
/// Viagens já partidas ou não ativas com participação aceite, ou aceite e depois cancelada por indisponibilização
/// (RF-F009-03, RF-F009-08). As canceladas pelo próprio passageiro ficam de fora até à decisão D-029.
/// </param>
/// <param name="PedidosEfetuados">Todos os pedidos feitos pelo utilizador, do mais recente para o mais antigo.</param>
/// <param name="PedidosRecebidos">Todos os pedidos recebidos nas viagens do utilizador.</param>
/// <param name="Notificacoes">Notificações recebidas, das mais recentes para as mais antigas.</param>
/// <param name="Avisos">Avisos das viagens em que o utilizador é condutor ou passageiro aceite.</param>
public sealed record HistoricoResultado(
    IReadOnlyList<ViagemHistorico> ViagensComoCondutor,
    IReadOnlyList<ViagemHistorico> ViagensComoPassageiro,
    IReadOnlyList<PedidoHistorico> PedidosEfetuados,
    IReadOnlyList<PedidoHistorico> PedidosRecebidos,
    IReadOnlyList<NotificacaoHistorico> Notificacoes,
    IReadOnlyList<AvisoHistorico> Avisos
);
