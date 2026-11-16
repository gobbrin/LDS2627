using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Consultas só de leitura do histórico pessoal (F009).</summary>
/// <remarks>
/// Devolve entidades sem seguimento; o <c>HistoricoService</c> monta as respostas da API (BE-07: o repositório não
/// conhece os DTOs).
/// </remarks>
public interface IRepositorioHistorico
{
    /// <summary>Viagens em que o utilizador foi condutor, da partida mais recente para a mais antiga (RF-F009-02, RF-F009-06).</summary>
    Task<IReadOnlyList<Viagem>> ListarViagensComoCondutorAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>
    /// Participações em que o utilizador chegou a ser aceite como passageiro, com a viagem carregada, da partida mais
    /// recente para a mais antiga (RF-F009-03, D-029).
    /// </summary>
    Task<IReadOnlyList<Participacao>> ListarParticipacoesComoPassageiroAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Pedidos feitos pelo utilizador, em todos os estados, com a viagem carregada, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<IReadOnlyList<Participacao>> ListarPedidosEfetuadosAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Pedidos recebidos nas viagens do utilizador, com a viagem carregada, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<IReadOnlyList<Participacao>> ListarPedidosRecebidosAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Notificações pessoais, das mais recentes para as mais antigas, sem paginação (RF-F009-05).</summary>
    Task<IReadOnlyList<Notificacao>> ListarNotificacoesAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Avisos das viagens em que o utilizador é condutor ou chegou a ser aceite como passageiro, mesmo que tenha cancelado depois (RF-F009-05, DEF-BE-024).</summary>
    Task<IReadOnlyList<Aviso>> ListarAvisosAsync(Guid utilizadorId, CancellationToken ct);
}
