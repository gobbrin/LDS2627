using System.IdentityModel.Tokens.Jwt;
using CampusCarpool.Application.Identidade;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CampusCarpool.Api.Configuration;

/// <summary>
/// Recusa os tokens revogados pelo fim de sessão (RF-F001-12, DEF-BE-004).
/// </summary>
/// <remarks>
/// Ligado ao evento <c>OnTokenValidated</c> do JwtBearer em <c>Program.cs</c>, por isso
/// aplica-se também ao hub SignalR. Um token sem <c>jti</c> não pode ser revogado e é recusado.
/// </remarks>
public static class ValidacaoTokensRevogados
{
    /// <summary>
    /// Chamado depois de a assinatura e a validade do token estarem verificadas; marca o pedido como não
    /// autenticado (401) se o token não tiver <c>jti</c> ou se esse <c>jti</c> tiver sido revogado.
    /// </summary>
    /// <param name="context">Contexto do evento do JwtBearer.</param>
    public static async Task ValidarAsync(TokenValidatedContext context)
    {
        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
        {
            context.Fail("Token sem identificador (jti).");
            return;
        }

        var autenticacao = context.HttpContext.RequestServices.GetRequiredService<IAutenticacaoService>();

        if (await autenticacao.TokenRevogadoAsync(jti, context.HttpContext.RequestAborted))
            context.Fail("Token revogado.");
    }
}
