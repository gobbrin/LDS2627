namespace CampusCarpool.Application.Avaliacoes;

/// <summary>
/// Classificação de um utilizador: só a média e o número de avaliações recebidas (RF-F007-08, RF-F007-10, BE-43).
/// </summary>
/// <remarks>
/// Os nomes são os do perfil (<c>classificacaoMedia</c>, <c>totalAvaliacoes</c>). Não há lista das avaliações: o produto
/// só mostra a média e o contador, e nunca o autor (P-22, RF-F007-07).
/// </remarks>
/// <param name="ClassificacaoMedia">Média arredondada a 1 casa decimal; <c>null</c> quando não há avaliações ("sem avaliações", RF-F007-10).</param>
/// <param name="TotalAvaliacoes">Número de avaliações recebidas.</param>
public sealed record ResumoAvaliacoes(decimal? ClassificacaoMedia, int TotalAvaliacoes);

/// <summary>Resposta a quem criou ou alterou uma avaliação.</summary>
/// <param name="Classificacao">Valor gravado.</param>
/// <param name="Data">Data de criação (não muda com a alteração, DO-10).</param>
public sealed record AvaliacaoRegistada(byte Classificacao, DateTime Data);
