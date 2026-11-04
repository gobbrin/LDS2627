using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Api.Controllers;

/// <summary>Avaliações entre participantes (F007). Rotas em <c>/api/v1/avaliacoes</c>.</summary>
/// <remarks>
/// As regras estão no <see cref="IAvaliacaoService"/>; aqui converte-se o tipo de exceção em código HTTP:
/// <see cref="ArgumentException"/> → 400, <see cref="KeyNotFoundException"/> → 404,
/// <see cref="InvalidOperationException"/> → 409 e <see cref="DbUpdateException"/> (índice único) → 409.
/// </remarks>
[ApiController, Authorize, Route("api/v1/avaliacoes")]
public sealed class AvaliacoesController(IAvaliacaoService avaliacoes) : ControllerBase
{
    /// <summary>Corpo de <c>POST</c>: viagem concluída, utilizador avaliado e valor de 1 a 5.</summary>
    public sealed record CriarAvaliacao(Guid ViagemId, Guid AvaliadoId, int Classificacao);
    /// <summary>Corpo de <c>PUT</c>: novo valor de 1 a 5.</summary>
    public sealed record AlterarAvaliacao(int Classificacao);
    /// <summary>Cria uma avaliação (RF-F007-01 a RF-F007-04). 201; 400; 401; 404; 409.</summary>
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
    /// <summary>Altera a avaliação do utilizador autenticado sobre <paramref name="avaliadoId"/> (RF-F007-05). 200; 400; 401; 404; 409.</summary>
    [HttpPut("utilizadores/{avaliadoId:guid}")]
    public async Task<IActionResult> Alterar(Guid avaliadoId, [FromBody] AlterarAvaliacao pedido, CancellationToken ct)
    {
        var id = IdAutenticado(); if (id is null) return Unauthorized();
        try { return Ok(await avaliacoes.AlterarAsync(id.Value, avaliadoId, pedido.Classificacao, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível alterar a avaliação." }); }
    }
    /// <summary>Avaliações recebidas por um utilizador, sem o autor, e a média (RF-F007-07, RF-F007-11). 200; 401; 404.</summary>
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
