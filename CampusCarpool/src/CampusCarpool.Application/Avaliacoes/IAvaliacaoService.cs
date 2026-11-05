namespace CampusCarpool.Application.Avaliacoes;

/// <summary>Avaliação recebida tal como é mostrada a qualquer utilizador: só o valor e a data, sem o autor (P-22, RF-F007-07).</summary>
/// <param name="Classificacao">Valor de 1 a 5.</param>
/// <param name="Data">Data de criação da avaliação.</param>
public sealed record AvaliacaoPublica(byte Classificacao, DateTime Data);

/// <summary>Avaliações recebidas por um utilizador e a respetiva média (RF-F007-08, RF-F007-11).</summary>
/// <param name="UtilizadorId">Utilizador avaliado.</param>
/// <param name="Total">Número de avaliações recebidas.</param>
/// <param name="Media">Média arredondada a 1 casa decimal; <c>null</c> quando não há avaliações ("sem avaliações", RF-F007-10).</param>
/// <param name="Avaliacoes">Lista das avaliações, sem o autor.</param>
public sealed record ResumoAvaliacoes(Guid UtilizadorId, int Total, decimal? Media, IReadOnlyList<AvaliacaoPublica> Avaliacoes);

/// <summary>Resposta a quem criou ou alterou uma avaliação.</summary>
/// <param name="Classificacao">Valor gravado.</param>
/// <param name="Data">Data de criação (não muda com a alteração, DO-10).</param>
public sealed record AvaliacaoRegistada(byte Classificacao, DateTime Data);

/// <summary>Regras das avaliações entre participantes (F007).</summary>
/// <remarks>
/// Os erros são comunicados por exceções genéricas, que o <c>AvaliacoesController</c> converte em HTTP:
/// <see cref="ArgumentException"/> → 400, <see cref="KeyNotFoundException"/> → 404,
/// <see cref="InvalidOperationException"/> → 409 (DO-09).
/// </remarks>
public interface IAvaliacaoService
{
    /// <summary>
    /// Cria a avaliação de <paramref name="avaliadoId"/> por <paramref name="avaliadorId"/> no contexto de uma viagem (RF-F007-01).
    /// </summary>
    /// <param name="viagemId">Viagem concluída em que ambos participaram.</param>
    /// <param name="avaliadorId">Autor (utilizador autenticado).</param>
    /// <param name="avaliadoId">Utilizador avaliado.</param>
    /// <param name="classificacao">Valor de 1 a 5.</param>
    /// <param name="ct">Cancelamento do pedido.</param>
    /// <exception cref="ArgumentException">Valor fora de 1 a 5, ou avaliar-se a si próprio (RF-F007-03).</exception>
    /// <exception cref="KeyNotFoundException">Viagem ou utilizador inexistente.</exception>
    /// <exception cref="InvalidOperationException">
    /// Viagem não concluída, algum dos dois não participou (P-16, RF-F007-02), ou já existe avaliação do par (RF-F007-04).
    /// </exception>
    Task<AvaliacaoRegistada> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);

    /// <summary>Substitui o valor da avaliação existente do par (RF-F007-05).</summary>
    /// <exception cref="ArgumentException">Valor fora de 1 a 5.</exception>
    /// <exception cref="KeyNotFoundException">Não existe avaliação deste avaliador sobre este avaliado.</exception>
    Task<AvaliacaoRegistada> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);

    /// <summary>Devolve as avaliações recebidas por um utilizador e a média (RF-F007-08 a RF-F007-11).</summary>
    /// <exception cref="KeyNotFoundException">Utilizador inexistente.</exception>
    Task<ResumoAvaliacoes> ConsultarAsync(Guid utilizadorId, CancellationToken ct);
}
