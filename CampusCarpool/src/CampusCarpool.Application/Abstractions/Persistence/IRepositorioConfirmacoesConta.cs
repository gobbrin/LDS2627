using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados dos tokens de confirmação de conta.</summary>
/// <remarks>Na BD só fica o SHA-256 do token (ver <see cref="ConfirmacaoConta"/>). Adicionar só fica gravado com a unidade de trabalho.</remarks>
public interface IRepositorioConfirmacoesConta
{
    /// <summary>Acrescenta um token de confirmação.</summary>
    void Adicionar(ConfirmacaoConta confirmacao);

    /// <summary>Obtém, para a alterar, a confirmação com este hash, em qualquer estado; <c>null</c> se não existir.</summary>
    /// <param name="tokenHash">SHA-256 do token.</param>
    /// <param name="ct">Cancelamento.</param>
    Task<ConfirmacaoConta?> ObterPorHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>Marca para apagar as ligações por usar do utilizador; só saem da BD com a unidade de trabalho.</summary>
    /// <param name="utilizadorId">Utilizador.</param>
    /// <param name="ct">Cancelamento.</param>
    Task RemoverPorUsarAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Data de expiração mais distante dos tokens do utilizador; <c>null</c> se não tiver nenhum.</summary>
    Task<DateTime?> ObterUltimaExpiracaoAsync(Guid utilizadorId, CancellationToken ct);
}
