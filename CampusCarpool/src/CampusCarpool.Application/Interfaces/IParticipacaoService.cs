namespace CampusCarpool.Application.Interfaces;

/// <summary>Resultados possíveis das operações de gestão de participações.</summary>
public enum ResultadoGestaoParticipacao
{
	Sucesso,
	NaoEncontrada,
	NaoAutorizado,
	Conflito,
	SemCapacidade,
	DadosInconsistentes
}

/// <summary>
/// Contrato usado pela API para gerir participações sem conhecer a base de dados.
/// </summary>
/// <remarks>
/// A implementação atual é <c>Infrastructure/Services/ParticipacaoService.cs</c>.
/// Notificações futuras devem ser publicadas depois de a operação transacional
/// ser confirmada, para não anunciar uma alteração que sofreu rollback.
/// </remarks>
public interface IParticipacaoService
{
	/// <summary>Solicita a aceitação de uma participação.</summary>
	Task<ResultadoGestaoParticipacao> AceitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
	/// <summary>Solicita a rejeição de uma participação pendente.</summary>
	Task<ResultadoGestaoParticipacao> RejeitarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
	/// <summary>Solicita o cancelamento de uma participação.</summary>
	Task<ResultadoGestaoParticipacao> CancelarAsync(Guid participacaoId, Guid utilizadorId, CancellationToken ct);
}
