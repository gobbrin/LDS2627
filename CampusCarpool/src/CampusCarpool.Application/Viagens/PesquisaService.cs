using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Rotas;

namespace CampusCarpool.Application.Viagens;

/// <summary>Implementação de <see cref="IPesquisaService"/>.</summary>
public sealed class PesquisaService(IRepositorioViagens viagens, IConsultaViagensService consultas) : IPesquisaService
{
    /// <inheritdoc />
    public async Task<ResultadoPesquisa> PesquisarAsync(FiltroPesquisaViagens filtro, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var pagina = await viagens.PesquisarAsync(filtro, DateTime.UtcNow, ct);

        // RF-F003-03 e RF-F003-04: pontos adicionais, nome e classificação média do condutor.
        var condutores = await consultas.CondutoresAsync(pagina.Viagens.Select(v => v.CondutorId).Distinct().ToList(), ct);

        var encontradas = pagina.Viagens
            .Select(v => new ViagemEncontrada(
                v.Id,
                v.Origem,
                v.Destino,
                v.PartidaEm,
                v.LugaresDisponiveis,
                v.DistanciaKm,
                v.DuracaoMinutos,
                RotaGuardada.ContarAdicionais(v.RotaPontos),
                condutores.GetValueOrDefault(v.CondutorId)))
            .ToList();

        return new ResultadoPesquisa(pagina.Total, filtro.Pagina, filtro.Tamanho, encontradas);
    }
}
