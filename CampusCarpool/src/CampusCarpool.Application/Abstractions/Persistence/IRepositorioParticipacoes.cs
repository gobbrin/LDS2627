using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>
/// Define as operações de acesso a dados relativas às participações.
/// A implementação concreta pertence à camada Infrastructure.
/// </summary>
/// <remarks>
/// As listas são só de leitura (não rastreadas); só <see cref="ObterPorIdAsync"/>,
/// <see cref="ListarPendentesParaAlterarAsync"/> e <see cref="ListarAtivasParaAlterarAsync"/> devolvem entidades para alterar.
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

    /// <summary>Indica se o passageiro tem algum pedido na viagem, em qualquer estado.</summary>
    Task<bool> ExistePedidoAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);

    /// <summary>Indica se o passageiro tem participação aceite na viagem.</summary>
    Task<bool> ExisteAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct);

    /// <summary>Lista os pedidos pendentes de uma viagem (rastreados, para serem alterados).</summary>
    Task<IReadOnlyList<Participacao>> ListarPendentesParaAlterarAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Lista os pedidos pendentes e as participações aceites de uma viagem (rastreados, para serem alterados).</summary>
    Task<IReadOnlyList<Participacao>> ListarAtivasParaAlterarAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Lista os passageiros aceites de uma viagem, pela ordem de aceitação.</summary>
    Task<IReadOnlyList<PassageiroAceite>> ListarPassageirosAceitesAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Lista os pedidos pendentes de uma viagem, do mais antigo para o mais recente.</summary>
    Task<IReadOnlyList<PedidoPendente>> ListarPedidosPendentesAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Grava um pedido novo.</summary>
    /// <returns><c>false</c> se a BD o recusar, por o passageiro já ter um pedido ou uma participação ativa na viagem.</returns>
    Task<bool> AdicionarPedidoAsync(Participacao pedido, CancellationToken ct);
}

/// <summary>Passageiro aceite de uma viagem. Um ponto a <c>null</c> segue a origem ou o destino da viagem (P-20).</summary>
public sealed record PassageiroAceite(Guid Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino);

/// <summary>Pedido pendente de uma viagem.</summary>
public sealed record PedidoPendente(Guid Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino, DateTime CriadoEm);
