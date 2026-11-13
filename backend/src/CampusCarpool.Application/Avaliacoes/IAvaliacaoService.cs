using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Avaliacoes;

/// <summary>Regras das avaliações entre participantes (F007).</summary>
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
    /// <returns>
    /// A avaliação; erro 400 com valor fora de 1 a 5 ou ao avaliar-se a si próprio (RF-F007-03); 404 se a viagem ou um
    /// dos utilizadores não existir; 409 com a viagem não concluída, se algum dos dois não participou (P-16,
    /// RF-F007-02) ou se já existe avaliação do par (RF-F007-04).
    /// </returns>
    Task<Resultado<AvaliacaoRegistada>> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);

    /// <summary>Substitui o valor da avaliação existente do par (RF-F007-05).</summary>
    /// <returns>A avaliação; erro 400 com valor fora de 1 a 5; 404 se não existir avaliação deste avaliador sobre este avaliado.</returns>
    Task<Resultado<AvaliacaoRegistada>> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct);

    /// <summary>Devolve as avaliações recebidas por um utilizador e a média (RF-F007-08 a RF-F007-11).</summary>
    /// <returns>O resumo, ou erro 404 se o utilizador não existir.</returns>
    Task<Resultado<ResumoAvaliacoes>> ConsultarAsync(Guid utilizadorId, CancellationToken ct);
}
