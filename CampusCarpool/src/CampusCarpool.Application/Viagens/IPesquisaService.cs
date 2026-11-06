using CampusCarpool.Application.Abstractions.Persistence;

namespace CampusCarpool.Application.Viagens;

/// <summary>Viagem nos resultados da pesquisa, com o condutor e os pontos adicionais (RF-F003-03, RF-F003-04).</summary>
public sealed record ViagemEncontrada(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresDisponiveis,
    decimal? DistanciaKm,
    int? DuracaoMinutos,
    int PontosAdicionais,
    CondutorViagem? Condutor);

/// <summary>Página de resultados da pesquisa.</summary>
public sealed record ResultadoPesquisa(int Total, int Pagina, int Tamanho, IReadOnlyList<ViagemEncontrada> Viagens);

/// <summary>Pesquisa de viagens (F003).</summary>
public interface IPesquisaService
{
    /// <summary>Pesquisa viagens ativas, futuras e com lugares (RF-F003-01, RF-F003-02, RF-F003-05, P-12).</summary>
    /// <remarks>Ordenado por partida crescente. A página e o tamanho já vêm validados pelo controller.</remarks>
    Task<ResultadoPesquisa> PesquisarAsync(FiltroPesquisaViagens filtro, CancellationToken ct);
}
