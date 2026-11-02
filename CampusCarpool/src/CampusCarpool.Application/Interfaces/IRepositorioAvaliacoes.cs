using CampusCarpool.Domain.Entities;
namespace CampusCarpool.Application.Interfaces;
public interface IRepositorioAvaliacoes
{
    Task<Viagem?> ObterViagemAsync(Guid viagemId, CancellationToken ct);
    Task<bool> UtilizadorExisteAsync(Guid id, CancellationToken ct);
    Task<bool> ParticipacaoAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);
    Task<Avaliacao?> ObterPorParAsync(Guid avaliadorId, Guid avaliadoId, CancellationToken ct);
    Task<Avaliacao> GuardarAsync(Avaliacao avaliacao, CancellationToken ct);
    Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct);
}
