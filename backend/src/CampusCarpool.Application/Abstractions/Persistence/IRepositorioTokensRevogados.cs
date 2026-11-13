using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados dos tokens revogados pelo fim de sessão (RF-F001-12).</summary>
/// <remarks>Adicionar só fica gravado com a unidade de trabalho; remover os expirados é imediato.</remarks>
public interface IRepositorioTokensRevogados
{
    /// <summary>Indica se o token com este <c>jti</c> foi revogado.</summary>
    Task<bool> EstaRevogadoAsync(string jti, CancellationToken ct);

    /// <summary>Acrescenta um token revogado.</summary>
    void Adicionar(TokenRevogado token);

    /// <summary>Apaga os tokens já expirados, que a validação da data recusa de qualquer forma.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <param name="ct">Cancelamento.</param>
    Task RemoverExpiradosAsync(DateTime agora, CancellationToken ct);
}
