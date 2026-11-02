
using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/historico")]
public sealed class HistoricoController(
    IHistoricoService historicoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar(CancellationToken ct)
    {
        var identificador =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(identificador, out var utilizadorId))
            return Unauthorized();

        var resultado = await historicoService.ConsultarAsync(
            utilizadorId, ct);

        return Ok(resultado);
    }
}
