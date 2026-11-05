namespace CampusCarpool.Application.Interfaces;

/// <summary>Resultados possíveis das operações de gestão de participações.</summary>
/// <remarks>O <c>GestaoParticipacoesController</c> traduz cada valor num código HTTP.</remarks>
public enum ResultadoGestaoParticipacao
{
    /// <summary>Operação feita (204).</summary>
    Sucesso,
    /// <summary>Participação ou viagem inexistente (404).</summary>
    NaoEncontrada,
    /// <summary>O utilizador não tem permissão para esta operação (403).</summary>
    NaoAutorizado,
    /// <summary>O estado atual não permite a operação: pedido já decidido, viagem não ativa ou participação aceite já depois da partida (409).</summary>
    Conflito,
    /// <summary>A viagem não tem lugares disponíveis (409, RF-F004-11).</summary>
    SemCapacidade,
    /// <summary><c>LugaresDisponiveis</c> não bate com os aceites contados; a operação é recusada para não agravar o erro (409).</summary>
    DadosInconsistentes,
    /// <summary>A aceitação não conseguiu recalcular a rota, por o Serviço de Mapas estar indisponível (503, P-19).</summary>
    RotaIndisponivel
}

/// <summary>
/// Contrato usado pela API para gerir participações sem conhecer a base de dados.
/// </summary>
/// <remarks>
/// A implementação atual é <c>Application/Services/ParticipacaoService.cs</c>.
/// Notificações futuras devem ser publicadas depois de a operação transacional
/// ser confirmada, para não anunciar uma alteração que sofreu rollback.
/// A criação do pedido não está aqui: é feita diretamente no <c>PedidosController</c>.
/// </remarks>
public interface IParticipacaoService
{
    /// <summary>Solicita a aceitação de uma participação.</summary>
    /// <remarks>
    /// Só o condutor, só pedidos pendentes de viagens ativas ainda por partir (RF-F004-10), e só com lugares
    /// livres (RF-F004-11). Ocupa um lugar e regista <c>AceiteEm</c>.
    /// </remarks>
    Task<ResultadoGestaoParticipacao> AceitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
    /// <summary>Solicita a rejeição de uma participação pendente.</summary>
    /// <remarks>Só o condutor (RF-F004-12).</remarks>
    Task<ResultadoGestaoParticipacao> RejeitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
    /// <summary>Solicita o cancelamento de uma participação.</summary>
    /// <remarks>
    /// Pendente ou aceite (RF-F004-15, RF-F004-16); a aceite só até à hora de partida (P-03) e liberta o lugar
    /// (RF-F004-17). Aceita o pedido do passageiro e também do condutor, o que nenhum requisito prevê (DO-06/DC-35).
    /// </remarks>
    Task<ResultadoGestaoParticipacao> CancelarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
}
