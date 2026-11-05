
using System.Security.Claims;
using CampusCarpool.Application.Historico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Histórico pessoal do utilizador autenticado (F009). Rota <c>GET /api/v1/historico</c>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/historico")]
public sealed class HistoricoController(
    IHistoricoService historicoService) : ControllerBase
{
    /// <summary>Devolve o <see cref="HistoricoResultado"/> do próprio utilizador (RF-F009-01 a RF-F009-08). 200; 401.</summary>
    /// <remarks>Não recebe id: cada utilizador só vê o seu histórico (RF-F009-07).</remarks>
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
