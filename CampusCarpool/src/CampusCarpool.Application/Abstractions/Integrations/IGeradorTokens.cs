namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Token de acesso emitido ao iniciar sessão.</summary>
/// <param name="AccessToken">O token.</param>
/// <param name="ExpiresIn">Validade, em segundos.</param>
public sealed record TokenAcesso(string AccessToken, int ExpiresIn);

/// <summary>Emite os tokens de acesso à API (P-17).</summary>
/// <remarks>Implementado por <c>Infrastructure/Security/GeradorTokensJwt.cs</c>.</remarks>
public interface IGeradorTokens
{
    /// <summary>Emite um token para o utilizador.</summary>
    TokenAcesso Gerar(Guid utilizadorId);
}
