namespace CampusCarpool.Application.Viagens;

/// <summary>
/// Conclusão automática das viagens (UC-F002-05; RF-F002-12, RF-F004-19).
/// </summary>
/// <remarks>
/// Implementado por <see cref="ConclusaoViagensService"/> e chamado periodicamente pelo
/// <c>TrabalhadorConclusaoViagens</c>. A conclusão manual (<c>POST /api/v1/viagens/{id}/concluir</c>)
/// continua a existir (PA-01/PA-03 por decidir pelo grupo).
/// </remarks>
public interface IConclusaoViagensService
{
    /// <summary>
    /// Passa a Concluida as viagens Ativas cuja hora de partida acrescida da duração
    /// estimada já passou em <paramref name="agora"/> (P-04) e cancela os pedidos
    /// ainda pendentes; as participações aceites mantêm-se. Devolve o número de
    /// viagens concluídas.
    /// </summary>
    /// <param name="agora">Momento de referência (UTC); recebido por parâmetro para os testes controlarem o relógio.</param>
    /// <param name="ct">Cancelamento (paragem da aplicação).</param>
    Task<int> ConcluirViagensTerminadasAsync(DateTime agora, CancellationToken ct);
}
