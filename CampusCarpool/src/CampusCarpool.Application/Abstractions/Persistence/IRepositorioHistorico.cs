using CampusCarpool.Application.Historico;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Consultas só de leitura do histórico pessoal (F009).</summary>
public interface IRepositorioHistorico
{
    /// <summary>Viagens em que o utilizador foi condutor, da partida mais recente para a mais antiga (RF-F009-02, RF-F009-06).</summary>
    Task<IReadOnlyList<ViagemHistorico>> ListarViagensComoCondutorAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Viagens em que o utilizador chegou a ser aceite como passageiro, da partida mais recente para a mais antiga (RF-F009-03, D-029).</summary>
    Task<IReadOnlyList<ViagemHistorico>> ListarViagensComoPassageiroAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Pedidos feitos pelo utilizador, em todos os estados, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<IReadOnlyList<PedidoHistorico>> ListarPedidosEfetuadosAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Pedidos recebidos nas viagens do utilizador, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<IReadOnlyList<PedidoHistorico>> ListarPedidosRecebidosAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Notificações pessoais, das mais recentes para as mais antigas, sem paginação (RF-F009-05).</summary>
    Task<IReadOnlyList<NotificacaoHistorico>> ListarNotificacoesAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Avisos das viagens em que o utilizador é condutor ou passageiro aceite (RF-F009-05).</summary>
    Task<IReadOnlyList<AvisoHistorico>> ListarAvisosAsync(Guid utilizadorId, CancellationToken ct);
}
