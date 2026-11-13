using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Viagens;

/// <summary>Implementação de <see cref="IConsultaViagensService"/>.</summary>
public sealed class ConsultaViagensService(
    IRepositorioParticipacoes participacoes,
    IRepositorioUtilizadores utilizadores,
    IRepositorioAvaliacoes avaliacoes) : IConsultaViagensService
{
    /// <inheritdoc />
    public async Task<bool> PodeConsultarAsync(Viagem viagem, Guid? utilizadorId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return PoliticaDeVisibilidade.VisivelSemPedido(viagem, utilizadorId, DateTime.UtcNow)
            || (utilizadorId is Guid id && await participacoes.ExistePedidoAsync(viagem.Id, id, ct));
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, CondutorViagem>> CondutoresAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var nomes = await utilizadores.ObterNomesAsync(ids, ct);
        var classificacoes = await avaliacoes.ListarClassificacoesAsync(ids, ct);

        var medias = classificacoes
            .GroupBy(c => c.AvaliadoId)
            .ToDictionary(g => g.Key, g => CalculadoraDeClassificacao.Media(g.Select(c => c.Classificacao)));

        return nomes.ToDictionary(
            n => n.Key,
            n => new CondutorViagem(n.Key, n.Value, medias.GetValueOrDefault(n.Key)));
    }
}
