namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Conclusão automática das viagens (UC-F002-05; RF-F002-12, RF-F004-19).
/// </summary>
public interface IConclusaoViagensService
{
    /// <summary>
    /// Passa a Concluida as viagens Ativas cuja hora de partida acrescida da duração
    /// estimada já passou em <paramref name="agora"/> (P-04) e cancela os pedidos
    /// ainda pendentes; as participações aceites mantêm-se. Devolve o número de
    /// viagens concluídas.
    /// </summary>
    Task<int> ConcluirViagensTerminadasAsync(DateTime agora, CancellationToken ct);
}
