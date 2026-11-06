using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusCarpool.Application.Abstractions.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CampusCarpool.Infrastructure.Security;

/// <summary>Emite tokens JWT assinados com a chave <c>Jwt:Key</c>.</summary>
/// <remarks>
/// O token (HS256) dura 60 minutos (P-17) e tem as claims <c>sub</c> (id do utilizador) e <c>jti</c> (identificador
/// único, usado no fim de sessão). Não há refresh token: expirado, é preciso novo login.
/// </remarks>
public sealed class GeradorTokensJwt(IConfiguration configuracao) : IGeradorTokens
{
    private const int MinutosDeValidade = 60;

    /// <inheritdoc />
    public TokenAcesso Gerar(Guid utilizadorId)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao["Jwt:Key"]!));

        var jwt = new JwtSecurityToken(
            configuracao["Jwt:Issuer"],
            configuracao["Jwt:Audience"],
            [
                new Claim(JwtRegisteredClaimNames.Sub, utilizadorId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(MinutosDeValidade),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new TokenAcesso(new JwtSecurityTokenHandler().WriteToken(jwt), MinutosDeValidade * 60);
    }
}
