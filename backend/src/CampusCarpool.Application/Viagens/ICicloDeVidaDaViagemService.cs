using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Viagens;

/// <summary>Fim do ciclo de vida de uma viagem, pelo condutor: indisponibilizar e concluir (F002).</summary>
/// <remarks>Implementada por <see cref="CicloDeVidaDaViagemService"/>; chamada pelo <c>ViagensController</c>.</remarks>
public interface ICicloDeVidaDaViagemService
{
    /// <summary>Indisponibiliza uma viagem ativa (RF-F002-09, RF-F002-10, P-25).</summary>
    /// <remarks>
    /// Cancela os pedidos pendentes e as participações aceites, liberta todos os lugares e notifica os passageiros
    /// afetados, não o condutor (P-23, RF-F006-03). A viagem fica no histórico dos aceites graças a <c>AceiteEm</c>.
    /// Permitido mesmo depois da hora de partida, desde que ainda esteja <c>Ativa</c>. Fica sem os pontos na rota; o
    /// Serviço de Mapas não impede a operação (P-19).
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="utilizadorId">Utilizador autenticado; tem de ser o condutor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>Sucesso; erro 403 se não for o condutor; 404 se a viagem não existir; 409 se já não estiver ativa.</returns>
    Task<Resultado> IndisponibilizarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Conclusão manual pelo condutor, depois da hora de partida.</summary>
    /// <remarks>
    /// Faz o mesmo que a conclusão automática (<see cref="IConclusaoViagensService"/>, RF-F002-12): passa a
    /// <c>Concluida</c> e cancela os pendentes (RF-F004-19); os aceites mantêm-se para a avaliação (P-16). Não envia
    /// notificações. Mantida até o grupo decidir PA-01/PA-03.
    /// </remarks>
    /// <param name="viagemId">Viagem.</param>
    /// <param name="utilizadorId">Utilizador autenticado; tem de ser o condutor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>Sucesso; erro 403 se não for o condutor; 404 se a viagem não existir; 409 se não estiver ativa ou ainda não tiver partido.</returns>
    Task<Resultado> ConcluirAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}
