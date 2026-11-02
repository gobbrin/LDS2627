namespace CampusCarpool.Application.Interfaces;
public sealed record AvaliacaoPublica(byte Classificacao, DateTime Data);
public sealed record ResumoAvaliacoes(Guid UtilizadorId, int Total, decimal? Media, IReadOnlyList<AvaliacaoPublica> Avaliacoes);
public sealed record AvaliacaoRegistada(byte Classificacao, DateTime Data);
public interface IAvaliacaoService
{
    Task<AvaliacaoRegistada> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);
    Task<AvaliacaoRegistada> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);
    Task<ResumoAvaliacoes> ConsultarAsync(Guid utilizadorId, CancellationToken ct);
}
