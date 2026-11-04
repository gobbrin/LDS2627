using System.IdentityModel.Tokens.Jwt;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Services;

/// <summary>
/// Recusa os tokens revogados pelo fim de sessão (RF-F001-12, DEF-BE-004).
/// </summary>
/// <remarks>
/// Ligado ao evento <c>OnTokenValidated</c> do JwtBearer em <c>Program.cs</c>, por isso
/// aplica-se também ao hub SignalR. Um token sem <c>jti</c> não pode ser revogado e é recusado.
/// </remarks>
public static class ValidacaoTokensRevogados
{
    public static async Task ValidarAsync(TokenValidatedContext context)
    {
        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
        {
            context.Fail("Token sem identificador (jti).");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<CampusCarpoolDbContext>();

        if (await db.TokensRevogados.AsNoTracking().AnyAsync(t => t.Jti == jti, context.HttpContext.RequestAborted))
            context.Fail("Token revogado.");
    }
}
