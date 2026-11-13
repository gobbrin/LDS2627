namespace CampusCarpool.Application.Viagens;

/// <summary>Pesquisa de viagens (F003).</summary>
public interface IPesquisaService
{
    /// <summary>Pesquisa viagens ativas, futuras e com lugares (RF-F003-01, RF-F003-02, RF-F003-05, P-12).</summary>
    /// <remarks>Ordenado por partida crescente. A página e o tamanho já vêm validados pelo controller.</remarks>
    Task<ResultadoPesquisa> PesquisarAsync(FiltroPesquisaViagens filtro, CancellationToken ct);
}
