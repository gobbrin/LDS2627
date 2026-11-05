using System.Security.Claims;
using CampusCarpool.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CampusCarpool.Api.Controllers;

/// <summary>Registo dos dispositivos Android para push (RF-F006-10). Rota <c>/api/v1/dispositivos</c>.</summary>
/// <remarks>
/// A aplicação Android chama <c>POST</c> depois do login com o token FCM. Ao terminar a sessão indica esse token
/// em <c>POST /autenticacao/logout</c>, que desativa o dispositivo (RF-F006-18); o <c>DELETE</c> também o desativa.
/// </remarks>
[ApiController, Authorize, Route("api/v1/dispositivos")]
public sealed class DispositivosController(IRepositorioDispositivos repositorio) : ControllerBase
{
    /// <summary>Corpo dos pedidos: o token FCM do dispositivo.</summary>
    public sealed record TokenDispositivo(string Token);
    private Guid? UtilizadorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) ? id : null;
    /// <summary>Regista (ou reativa) o dispositivo para o utilizador autenticado. 204; 400 token inválido; 401.</summary>
    [HttpPost]
    public async Task<IActionResult> Registar([FromBody] TokenDispositivo input, CancellationToken ct)
    {
        if (UtilizadorId is not Guid id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Token) || input.Token.Length > 4096)
            return BadRequest(new { erro = "Token FCM inválido." });
        await repositorio.RegistarAsync(id, input.Token.Trim(), ct);
        return NoContent();
    }
    /// <summary>Desativa o dispositivo (deixa de receber push). 204 mesmo que o token não exista; 400; 401.</summary>
    [HttpDelete]
    public async Task<IActionResult> Remover([FromBody] TokenDispositivo input, CancellationToken ct)
    {
        if (UtilizadorId is not Guid id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Token)) return BadRequest();
        await repositorio.DesativarAsync(id, input.Token.Trim(), ct);
        return NoContent();
    }
}
