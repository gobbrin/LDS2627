using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados das viagens usado pelos serviços da Application.</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Repositories/RepositorioViagens.cs</c>. A criação e a edição de viagens não
/// passam por aqui: o <c>ViagensController</c> usa o <c>DbContext</c>.
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

    /// <summary>Pesquisa as viagens ativas, com lugares e ainda por partir, por partida crescente (P-12).</summary>
    /// <param name="filtro">Filtros e paginação.</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>O total de resultados e as viagens da página pedida.</returns>
    Task<PaginaViagens> PesquisarAsync(FiltroPesquisaViagens filtro, DateTime agora, CancellationToken ct);
}

/// <summary>Viagem ativa já partida: identificador, partida e duração estimada (P-04), se existir.</summary>
public sealed record ViagemEmCurso(Guid Id, DateTime PartidaEm, int? DuracaoMinutos);

/// <summary>Filtros da pesquisa de viagens. Os nulos não filtram.</summary>
/// <param name="Origem">Texto contido na origem, sem distinguir maiúsculas nem acentos.</param>
/// <param name="Destino">Texto contido no destino, nas mesmas condições.</param>
/// <param name="Data">Dia da partida.</param>
/// <param name="Hora">Hora mínima da partida.</param>
/// <param name="Pagina">Página, a contar de 1.</param>
/// <param name="Tamanho">Viagens por página.</param>
public sealed record FiltroPesquisaViagens(
    string? Origem,
    string? Destino,
    DateOnly? Data,
    TimeOnly? Hora,
    int Pagina,
    int Tamanho);

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
