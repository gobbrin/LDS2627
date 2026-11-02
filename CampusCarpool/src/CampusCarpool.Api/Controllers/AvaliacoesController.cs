using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Api.Controllers;
[ApiController, Authorize, Route("api/v1/avaliacoes")]
public sealed class AvaliacoesController(IAvaliacaoService avaliacoes) : ControllerBase
{
    public sealed record CriarAvaliacao(Guid ViagemId, Guid AvaliadoId, int Classificacao);
    public sealed record AlterarAvaliacao(int Classificacao);
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarAvaliacao pedido, CancellationToken ct)
    {
        var id = IdAutenticado(); if (id is null) return Unauthorized();
        try { return StatusCode(201, await avaliacoes.CriarAsync(pedido.ViagemId, id.Value, pedido.AvaliadoId, pedido.Classificacao, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível registar a avaliação; poderá já existir." }); }
    }
    [HttpPut("utilizadores/{avaliadoId:guid}")]
    public async Task<IActionResult> Alterar(Guid avaliadoId, [FromBody] AlterarAvaliacao pedido, CancellationToken ct)
    {
        var id = IdAutenticado(); if (id is null) return Unauthorized();
        try { return Ok(await avaliacoes.AlterarAsync(id.Value, avaliadoId, pedido.Classificacao, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível alterar a avaliação." }); }
    }
    [HttpGet("utilizadores/{utilizadorId:guid}")]
    public async Task<IActionResult> Consultar(Guid utilizadorId, CancellationToken ct)
    {
        if (IdAutenticado() is null) return Unauthorized();
        try { return Ok(await avaliacoes.ConsultarAsync(utilizadorId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { erro = ex.Message }); }
    }
    private Guid? IdAutenticado() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
}
