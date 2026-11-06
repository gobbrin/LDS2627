using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados das viagens usado pelos serviços da Application.</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioViagens.cs</c>.</remarks>
public interface IRepositorioViagens
{
    /// <summary>
    /// Obtém uma viagem pelo identificador.
    /// A implementação deve permitir atualizar a entidade obtida.
    /// </summary>
    Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Lê o estado atual na BD, mesmo que a viagem já esteja carregada no pedido; <c>null</c> se não existir.</summary>
    Task<EstadoViagem?> ObterEstadoAsync(Guid viagemId, CancellationToken ct);

    /// <summary>Lista as viagens ativas cuja partida já passou, só com o necessário para ver se terminaram.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <param name="ct">Cancelamento.</param>
    Task<IReadOnlyList<ViagemEmCurso>> ListarAtivasJaPartidasAsync(DateTime agora, CancellationToken ct);

    /// <summary>Pesquisa as viagens ativas, com lugares e ainda por partir, por partida crescente (P-12).</summary>
    /// <param name="filtro">Filtros e paginação.</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>O total de resultados e as viagens da página pedida.</returns>
    Task<PaginaViagens> PesquisarAsync(FiltroPesquisaViagens filtro, DateTime agora, CancellationToken ct);

    /// <summary>Lista as viagens ativas do condutor, por ordem de partida.</summary>
    Task<IReadOnlyList<ViagemDoCondutor>> ListarAtivasDoCondutorAsync(Guid condutorId, CancellationToken ct);

    /// <summary>Acrescenta uma viagem; só fica gravada com a unidade de trabalho.</summary>
    void Adicionar(Viagem viagem);
}

/// <summary>Viagem ativa já partida: identificador, partida e duração estimada (P-04), se existir.</summary>
public sealed record ViagemEmCurso(Guid Id, DateTime PartidaEm, int? DuracaoMinutos);

/// <summary>Dados de uma viagem usados nos resultados da pesquisa.</summary>
public sealed record ViagemPesquisada(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresDisponiveis,
    decimal? DistanciaKm,
    int? DuracaoMinutos,
    Guid CondutorId,
    string? RotaPontos);

/// <summary>Total de resultados da pesquisa e as viagens da página pedida.</summary>
public sealed record PaginaViagens(int Total, IReadOnlyList<ViagemPesquisada> Viagens);
