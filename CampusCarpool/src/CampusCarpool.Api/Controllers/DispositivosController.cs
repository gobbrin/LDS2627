using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CampusCarpool.Api.Controllers;
[ApiController, Authorize, Route("api/v1/dispositivos")]
public sealed class DispositivosController(IRepositorioDispositivos repositorio) : ControllerBase
{
    public sealed record TokenDispositivo(string Token);
    private Guid? UtilizadorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) ? id : null;
    [HttpPost]
    public async Task<IActionResult> Registar([FromBody] TokenDispositivo input, CancellationToken ct)
    {
        if (UtilizadorId is not Guid id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Token) || input.Token.Length > 4096)
            return BadRequest(new { erro = "Token FCM inválido." });
        await repositorio.RegistarAsync(id, input.Token.Trim(), ct);
        return NoContent();
    }
    [HttpDelete]
    public async Task<IActionResult> Remover([FromBody] TokenDispositivo input, CancellationToken ct)
    {
        if (UtilizadorId is not Guid id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Token)) return BadRequest();
        await repositorio.DesativarAsync(id, input.Token.Trim(), ct);
        return NoContent();
    }
}
