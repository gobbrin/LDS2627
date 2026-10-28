using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Define as operações de acesso a dados relativas às participações.
/// A implementação concreta pertence à camada Infrastructure.
/// </summary>
public interface IRepositorioParticipacoes {
    
    ///<summary>
    /// Obtém uma participação pelo identificador.
    /// A implementação deve permitir atualizar a entidade obtida.
    /// </summary>
    Task<Participacao?> ObterPorIdAsync(Guid participacaoId, CancellationToken ct);

    ///<summary>
    /// Lista as participações de uma viagem.
    /// Quando o estado é null, considera todos os estados.
    /// </summary>
    Task<IReadOnlyList<Participacao>> ListarPorViagemAsync(Guid viagemId, EstadoParticipacao? estado, CancellationToken ct);

    ///<summary>
    /// Lista as participações de um passageiro.
    /// </summary>
    Task<IReadOnlyList<Participacao>> ListarPorPassageiroAsync(Guid passageiroId, CancellationToken ct);

    ///<summary>
    /// Verifica se existe uma participação pendente ou aceite
    /// para o passageiro na viagem indicada.
    /// </summary>
    Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);

}