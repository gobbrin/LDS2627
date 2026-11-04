using CampusCarpool.Domain.Entities;
namespace CampusCarpool.Application.Interfaces;

/// <summary>Acesso a dados das avaliações e das consultas de que o <c>AvaliacaoService</c> precisa.</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioAvaliacoes.cs</c>.</remarks>
public interface IRepositorioAvaliacoes
{
    /// <summary>Obtém a viagem, só para leitura; <c>null</c> se não existir.</summary>
    Task<Viagem?> ObterViagemAsync(Guid viagemId, CancellationToken ct);
    /// <summary>Indica se o utilizador existe.</summary>
    Task<bool> UtilizadorExisteAsync(Guid id, CancellationToken ct);
    /// <summary>Indica se o passageiro tem participação <c>Aceite</c> na viagem (elegibilidade, P-16).</summary>
    Task<bool> ParticipacaoAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);
    /// <summary>Obtém a avaliação atual do par (avaliador, avaliado), em qualquer viagem (RF-F007-04); fica rastreada para ser alterada.</summary>
    Task<Avaliacao?> ObterPorParAsync(Guid avaliadorId, Guid avaliadoId, CancellationToken ct);
    /// <summary>Insere a avaliação, se for nova, e grava as alterações.</summary>
    Task<Avaliacao> GuardarAsync(Avaliacao avaliacao, CancellationToken ct);
    /// <summary>Lista as avaliações recebidas pelo utilizador, das mais recentes para as mais antigas.</summary>
    Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct);
}
