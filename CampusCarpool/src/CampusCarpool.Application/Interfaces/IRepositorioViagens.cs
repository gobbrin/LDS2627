using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Acesso a dados das viagens usado pelo <c>ParticipacaoService</c>.</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Repositories/RepositorioViagens.cs</c>. A criação, a edição e a pesquisa
/// de viagens não passam por aqui: o <c>ViagensController</c> e o <c>PesquisaController</c> usam o <c>DbContext</c>.
/// </remarks>
public interface IRepositorioViagens
{
    /// <summary>
    /// Obtém uma viagem pelo identificador.
    /// A implementação deve permitir atualizar a entidade obtida.
    /// </summary>
    Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct);

}
