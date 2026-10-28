using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace CampusCarpool.Api.Controllers;

[ApiController, Route("api/v1/autenticacao")]
public sealed class AutenticacaoController(CampusCarpoolDbContext db, IConfiguration config, IServicoEmail emailSender) : ControllerBase
{
    public sealed record Registo(string Nome, string Email, string Password);
    public sealed record Login(string Email, string Password);
    public sealed record Confirmar(string Token);
    [HttpPost("registo")]
    public async Task<IActionResult> Registar(Registo input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Nome) || input.Nome.Length > 100 ||
            string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 12)
            return BadRequest(new { erro = "Dados de registo inválidos." });
        var email = input.Email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@estg.ipp.pt", StringComparison.OrdinalIgnoreCase) || email.Length > 255)
            return BadRequest(new { erro = "É necessário um email académico permitido." });
        if (await db.Utilizadores.AnyAsync(x => x.Email == email, ct)) return Conflict(new { erro = "Email já registado." });
        var user = new Utilizador { Nome = input.Nome.Trim(), Email = email };
        user.PasswordHash = new PasswordHasher<Utilizador>().HashPassword(user, input.Password);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.Utilizadores.Add(user);
        var confirmacao = new ConfirmacaoConta { UtilizadorId = user.Id, TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), ExpiraEm = DateTime.UtcNow.AddHours(24) };
        db.ConfirmacoesConta.Add(confirmacao);
        await db.SaveChangesAsync(ct);
        try { await emailSender.EnviarConfirmacaoAsync(email, token, ct); }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            // Não marcar como confirmado nem revelar o token; permitir reenvio após configurar SMTP.
            return StatusCode(503, new { erro = "Conta criada, mas o envio do email falhou. Contacta o suporte para reenviar a confirmação." });
        }
        return Accepted(new { mensagem = "Verifica o email académico para confirmar a conta." });
    }
    public sealed record ReenviarConfirmacao(string Email);

    // Resposta neutra para não revelar se o endereço existe na plataforma.
    [HttpPost("reenviar-confirmacao")]
    public async Task<IActionResult> ReenviarConfirmacaoEmail(ReenviarConfirmacao input, CancellationToken ct)
    {
        const string resposta = "Se existir uma conta pendente associada a este email, receberás uma nova ligação de confirmação.";
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 255)
            return BadRequest(new { erro = "Email inválido." });

        var email = input.Email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@estg.ipp.pt", StringComparison.OrdinalIgnoreCase))
            return Accepted(new { mensagem = resposta });

        var utilizador = await db.Utilizadores.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (utilizador is null || utilizador.EmailConfirmado)
            return Accepted(new { mensagem = resposta });

        // Limita reenvios a um por minuto, aproveitando o instante de criação do token.
        var agora = DateTime.UtcNow;
        var ultimo = await db.ConfirmacoesConta
            .Where(c => c.UtilizadorId == utilizador.Id)
            .OrderByDescending(c => c.ExpiraEm)
            .Select(c => (DateTime?)c.ExpiraEm)
            .FirstOrDefaultAsync(ct);
        if (ultimo.HasValue && ultimo.Value > agora.AddHours(24).AddMinutes(-1))
            return Accepted(new { mensagem = resposta });

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        db.ConfirmacoesConta.Add(new ConfirmacaoConta
        {
            UtilizadorId = utilizador.Id,
            TokenHash = hash,
            ExpiraEm = agora.AddHours(24)
        });
        await db.SaveChangesAsync(ct);

        try
        {
            await emailSender.EnviarConfirmacaoAsync(email, token, ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "[SMTP ERRO] " + ex.ToString());

            return Accepted(new { mensagem = resposta });
        }

        return Accepted(new { mensagem = resposta });
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> ConfirmarConta(Confirmar input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Token)) return BadRequest();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Token)));
        var confirmacao = await db.ConfirmacoesConta.FirstOrDefaultAsync(x => x.TokenHash == hash && x.ConsumidoEm == null && x.ExpiraEm > DateTime.UtcNow, ct);
        if (confirmacao is null) return BadRequest(new { erro = "Token inválido ou expirado." });
        var user = await db.Utilizadores.FindAsync([confirmacao.UtilizadorId], ct);
        if (user is null) return NotFound();
        user.EmailConfirmado = true; confirmacao.ConsumidoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPost("login")]
    public async Task<IActionResult> IniciarSessao(Login input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Unauthorized();
        var normalized = input.Email.Trim().ToLowerInvariant();
        var user = await db.Utilizadores.FirstOrDefaultAsync(x => x.Email == normalized, ct);
        if (user is null || new PasswordHasher<Utilizador>().VerifyHashedPassword(user, user.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { erro = "Credenciais inválidas." });
        if (!user.EmailConfirmado) return StatusCode(403, new { erro = "Conta por confirmar." });
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(60), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(jwt), expiresIn = 3600 });
    }
    [Authorize, HttpGet("me")]
    public IActionResult Me() => Ok(new { id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") });
}
