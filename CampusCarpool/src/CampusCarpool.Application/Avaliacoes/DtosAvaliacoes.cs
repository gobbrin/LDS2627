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
