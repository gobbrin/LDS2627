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

/// <summary>
/// Registo, confirmação de conta, início e fim de sessão (F001). Rotas em <c>/api/v1/autenticacao</c>.
/// </summary>
/// <remarks>
/// Usa o <see cref="CampusCarpoolDbContext"/> diretamente (sem serviço da Application; DC-39). As palavras-passe
/// são guardadas com o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core Identity (PBKDF2). Os tokens de
/// confirmação são 32 bytes aleatórios; na BD só fica o SHA-256 (ver <see cref="ConfirmacaoConta"/>).
/// O domínio académico (<c>@estg.ipp.pt</c>) está fixo no código em vez de vir da configuração (P-01, DEF-BE-002).
/// </remarks>
[ApiController, Route("api/v1/autenticacao")]
public sealed class AutenticacaoController(CampusCarpoolDbContext db, IConfiguration config, IServicoEmail emailSender) : ControllerBase
{
    /// <summary>Corpo de <c>POST registo</c>.</summary>
    public sealed record Registo(string Nome, string Email, string Password);
    /// <summary>Corpo de <c>POST login</c>.</summary>
    public sealed record Login(string Email, string Password);
    /// <summary>Corpo de <c>POST confirmar</c>: o token recebido na ligação do email.</summary>
    public sealed record Confirmar(string Token);

    // P-08 / RF-F001-05: mínimo de 8 caracteres, com pelo menos uma letra e um dígito.
    private static bool PalavraPasseValida(string? palavraPasse) =>
        palavraPasse is { Length: >= 8 } && palavraPasse.Any(char.IsLetter) && palavraPasse.Any(char.IsDigit);

    /// <summary>Cria uma conta por confirmar e envia a ligação de confirmação (RF-F001-02 a RF-F001-06).</summary>
    /// <returns>
    /// 202 conta criada e email enviado; 400 dados inválidos ou email não académico (RF-F001-03); 409 email já
    /// registado (RF-F001-04); 503 conta criada mas o envio do email falhou.
    /// </returns>
    [HttpPost("registo")]
    public async Task<IActionResult> Registar(Registo input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Nome) || input.Nome.Length > 100 ||
            string.IsNullOrWhiteSpace(input.Email) || !PalavraPasseValida(input.Password))
            return BadRequest(new { erro = "Dados de registo inválidos." });
        // Emails comparados sempre em minúsculas e sem espaços (o login normaliza da mesma forma).
        var email = input.Email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@estg.ipp.pt", StringComparison.OrdinalIgnoreCase) || email.Length > 255)
            return BadRequest(new { erro = "É necessário um email académico permitido." });
        // API-11: o 409 revela que o email já tem conta (enumeração de contas), ao contrário do reenvio.
        if (await db.Utilizadores.AnyAsync(x => x.Email == email, ct)) return Conflict(new { erro = "Email já registado." });
        var user = new Utilizador { Nome = input.Nome.Trim(), Email = email };
        user.PasswordHash = new PasswordHasher<Utilizador>().HashPassword(user, input.Password);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.Utilizadores.Add(user);
        var confirmacao = new ConfirmacaoConta { UtilizadorId = user.Id, TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), ExpiraEm = DateTime.UtcNow.AddHours(24) };
        db.ConfirmacoesConta.Add(confirmacao);
        // A conta fica gravada antes do envio: se o email falhar, o utilizador pode pedir o reenvio.
        await db.SaveChangesAsync(ct);
        // DC-43 / IN-03 (fica para m4): a Brevo recusar o envio lança HttpRequestException, que não é
        // apanhada aqui e chega ao cliente como 500, em vez do 503.
        try { await emailSender.EnviarConfirmacaoAsync(email, token, ct); }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            // Não marcar como confirmado nem revelar o token; permitir reenvio após configurar SMTP.
            return StatusCode(503, new { erro = "Conta criada, mas o envio do email falhou. Contacta o suporte para reenviar a confirmação." });
        }
        return Accepted(new { mensagem = "Verifica o email académico para confirmar a conta." });
    }
    /// <summary>Corpo de <c>POST reenviar-confirmacao</c>.</summary>
    public sealed record ReenviarConfirmacao(string Email);

    /// <summary>Envia uma nova ligação de confirmação a uma conta por confirmar (RF-F001-21).</summary>
    /// <remarks>
    /// Resposta neutra para não revelar se o endereço existe na plataforma.
    /// No máximo um reenvio por minuto. As ligações anteriores continuam válidas até expirarem (API-12).
    /// </remarks>
    /// <returns>202 sempre que o email tem formato aceitável (exista ou não a conta); 400 email vazio ou longo.</returns>
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
        // Não há data de criação (DO-10): como a validade é sempre 24 h, ExpiraEm - 24 h é a criação.
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
            // A falha não é revelada ao cliente (resposta neutra); só fica no Debug, não no log da aplicação.
            System.Diagnostics.Debug.WriteLine(
                "[SMTP ERRO] " + ex.ToString());

            return Accepted(new { mensagem = resposta });
        }

        return Accepted(new { mensagem = resposta });
    }

    /// <summary>Confirma a conta com o token da ligação do email (RF-F001-07, RF-F001-08).</summary>
    /// <remarks>
    /// O token é procurado pelo hash e só serve se não foi usado nem expirou (24 h, P-02). Expirado e já usado dão a
    /// mesma resposta; RF-F001-08 pede que o utilizador saiba o motivo (DEF-BE-003, fica para m4).
    /// </remarks>
    /// <returns>204 conta confirmada; 400 token em falta, inválido, expirado ou já usado; 404 utilizador inexistente.</returns>
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
    /// <summary>Inicia sessão e devolve um JWT de acesso (RF-F001-10, RF-F001-11, P-09, P-17).</summary>
    /// <remarks>
    /// O token (HS256, chave <c>Jwt:Key</c>) dura 60 minutos e tem as claims <c>sub</c> (id do utilizador) e
    /// <c>jti</c> (identificador único, usado no fim de sessão). Não há refresh token: expirado, é preciso novo login.
    /// </remarks>
    /// <returns>
    /// 200 <c>{ accessToken, expiresIn }</c>; 401 credenciais inválidas, com a mesma mensagem para email inexistente e
    /// palavra-passe errada (RF-F001-11); 403 conta por confirmar (P-09).
    /// </returns>
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
    /// <summary>
    /// Termina a sessão: revoga o token atual, que deixa de ser aceite (401) até expirar
    /// (RF-F001-12, DEF-BE-004). Devolve 204; sem token válido, 401.
    /// </summary>
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> TerminarSessao(CancellationToken ct)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrEmpty(jti)) return Unauthorized();

        // Sem "exp" legível, guarda-se pela duração máxima do token (60 min).
        var expiraEm = long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
            : DateTime.UtcNow.AddMinutes(60);

        // Limpeza: um token expirado já é recusado pela validação da data.
        var agora = DateTime.UtcNow;
        await db.TokensRevogados.Where(t => t.ExpiraEm < agora).ExecuteDeleteAsync(ct);

        db.TokensRevogados.Add(new TokenRevogado { Jti = jti, ExpiraEm = expiraEm });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    /// <summary>Devolve o id do utilizador autenticado (útil para o frontend validar o token). 200; 401 sem sessão.</summary>
    /// <remarks>O perfil completo está em <c>GET /api/v1/utilizadores/me</c>.</remarks>
    [Authorize, HttpGet("me")]
    public IActionResult Me() => Ok(new { id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") });
}
