using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusCarpool.Api.Configuration;
using CampusCarpool.Application.Identidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Registo, confirmação de conta, início e fim de sessão (F001). Rotas em <c>/api/v1/autenticacao</c>.
/// </summary>
[ApiController, Route("api/v1/autenticacao")]
public sealed class AutenticacaoController(IAutenticacaoService autenticacao) : ControllerBase
{
    private const string MensagemReenvio = "Se existir uma conta pendente associada a este email, receberás uma nova ligação de confirmação.";

    /// <summary>Corpo de <c>POST registo</c>.</summary>
    public sealed record Registo(string Nome, string Email, string Password);
    /// <summary>Corpo de <c>POST login</c>.</summary>
    public sealed record Login(string Email, string Password);
    /// <summary>Corpo de <c>POST confirmar</c>: o token recebido na ligação do email.</summary>
    public sealed record Confirmar(string Token);
    /// <summary>Corpo de <c>POST reenviar-confirmacao</c>.</summary>
    public sealed record ReenviarConfirmacao(string Email);
    /// <summary>Corpo opcional de <c>POST logout</c>: a aplicação Android indica o seu token FCM; o web não envia nada.</summary>
    public sealed record TerminarSessaoPedido(string? TokenDispositivo);

    /// <summary>Cria uma conta por confirmar e envia a ligação de confirmação (RF-F001-02 a RF-F001-06).</summary>
    /// <returns>
    /// 202 conta criada e email enviado; 400 dados inválidos ou email não académico (RF-F001-03); 409 email já
    /// registado (RF-F001-04); 503 conta criada mas o envio do email falhou.
    /// </returns>
    [HttpPost("registo"), EnableRateLimiting(LimitesPedidos.Autenticacao)]
    public async Task<IActionResult> Registar(Registo input, CancellationToken ct) =>
        this.Responder(
            await autenticacao.RegistarAsync(input.Nome, input.Email, input.Password, ct),
            () => Accepted(new RespostaMensagem("Verifica o email académico para confirmar a conta.")));

    /// <summary>Envia uma nova ligação de confirmação a uma conta por confirmar (RF-F001-21).</summary>
    /// <remarks>
    /// Resposta neutra para não revelar se o endereço existe na plataforma.
    /// No máximo um reenvio por minuto. Cada reenvio invalida as ligações anteriores por usar.
    /// </remarks>
    /// <returns>202 sempre que o email tem formato aceitável (exista ou não a conta); 400 email vazio ou longo.</returns>
    [HttpPost("reenviar-confirmacao"), EnableRateLimiting(LimitesPedidos.Autenticacao)]
    public async Task<IActionResult> ReenviarConfirmacaoEmail(ReenviarConfirmacao input, CancellationToken ct) =>
        this.Responder(
            await autenticacao.ReenviarConfirmacaoAsync(input.Email, ct),
            () => Accepted(new RespostaMensagem(MensagemReenvio)));

    /// <summary>Confirma a conta com o token da ligação do email (RF-F001-07, RF-F001-08).</summary>
    /// <remarks>Ver <see cref="IAutenticacaoService.ConfirmarContaAsync"/>.</remarks>
    /// <returns>204 conta confirmada; 400 token em falta, inválido, expirado ou já usado; 404 utilizador inexistente.</returns>
    [HttpPost("confirmar")]
    public async Task<IActionResult> ConfirmarConta(Confirmar input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Token)) return BadRequest();

        return this.Responder(await autenticacao.ConfirmarContaAsync(input.Token, ct), () => NoContent());
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
    [HttpPost("login"), EnableRateLimiting(LimitesPedidos.Autenticacao)]
    public async Task<IActionResult> IniciarSessao(Login input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Unauthorized();

        return this.Responder(
            await autenticacao.IniciarSessaoAsync(input.Email, input.Password, ct),
            token => Ok(token));
    }

    /// <summary>
    /// Termina a sessão: revoga o token atual, que deixa de ser aceite (401) até expirar
    /// (RF-F001-12, DEF-BE-004), e desativa o dispositivo indicado, sem afetar os outros (RF-F006-18, D-020).
    /// Devolve 204; sem token válido, 401.
    /// </summary>
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> TerminarSessao(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TerminarSessaoPedido? input,
        CancellationToken ct)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrEmpty(jti)) return Unauthorized();

        DateTime? expiraEm = long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
            : null;

        var utilizadorId = User.IdUtilizador();

        await autenticacao.TerminarSessaoAsync(jti, expiraEm, utilizadorId, input?.TokenDispositivo, ct);

        return NoContent();
    }

    /// <summary>Devolve o id do utilizador autenticado (útil para o frontend validar o token). 200; 401 sem sessão.</summary>
    /// <remarks>O perfil completo está em <c>GET /api/v1/utilizadores/me</c>.</remarks>
    [Authorize, HttpGet("me")]
    public IActionResult Me() => Ok(new SessaoAtual(User.TextoDoId()));
}
