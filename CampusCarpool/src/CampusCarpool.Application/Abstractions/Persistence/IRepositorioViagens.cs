using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados das viagens usado pelos serviços da Application.</summary>
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

    /// <summary>Lista as viagens ativas cuja partida já passou, só com o necessário para ver se terminaram.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <param name="ct">Cancelamento.</param>
    Task<IReadOnlyList<ViagemEmCurso>> ListarAtivasJaPartidasAsync(DateTime agora, CancellationToken ct);
}

/// <summary>Viagem ativa já partida: identificador, partida e duração estimada (P-04), se existir.</summary>
public sealed record ViagemEmCurso(Guid Id, DateTime PartidaEm, int? DuracaoMinutos);
