using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>
/// Define as operações de acesso a dados relativas às participações.
/// A implementação concreta pertence à camada Infrastructure.
/// </summary>
/// <remarks>
/// As listas são só de leitura (não rastreadas); só <see cref="ObterPorIdAsync"/> e
/// <see cref="ListarPendentesParaAlterarAsync"/> devolvem entidades para alterar.
/// </remarks>
public interface IRepositorioParticipacoes
{

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
    /// <remarks>RF-F004-06. O índice único filtrado <c>(ViagemId, PassageiroId)</c> garante o mesmo na BD (I038).</remarks>
    Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);

    /// <summary>Lista os pedidos pendentes de uma viagem (rastreados, para serem alterados).</summary>
    Task<IReadOnlyList<Participacao>> ListarPendentesParaAlterarAsync(Guid viagemId, CancellationToken ct);

}
