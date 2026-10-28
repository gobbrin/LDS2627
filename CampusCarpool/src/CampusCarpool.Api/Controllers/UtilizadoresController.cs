
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/utilizadores")]
public sealed class UtilizadoresController(
    CampusCarpoolDbContext db) : ControllerBase
{
    public sealed record AtualizarPerfil(
        string Nome,
        string? Curso,
        string? Instituicao
    );

    private Guid? ObterUtilizadorId()
    {
        var valor =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(valor, out var id) ? id : null;
    }

    // GET /api/v1/utilizadores/me
    [HttpGet("me")]
    public async Task<IActionResult> ConsultarPerfil(
        CancellationToken ct)
    {
        var id = ObterUtilizadorId();

        if (id is null)
            return Unauthorized();

        var perfil = await db.Utilizadores
            .AsNoTracking()
            .Where(u => u.Id == id.Value)
            .Select(u => new
            {
                u.Id,
                u.Nome,
                u.Email,
                u.EmailConfirmado,
                u.Curso,
                u.Instituicao,
                u.CriadoEm
            })
            .FirstOrDefaultAsync(ct);

        if (perfil is null)
            return NotFound(new
            {
                erro = "Utilizador não encontrado."
            });

        return Ok(perfil);
    }

    // PUT /api/v1/utilizadores/me
    [HttpPut("me")]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarPerfil input,
        CancellationToken ct)
    {
        var id = ObterUtilizadorId();

        if (id is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(input.Nome)
            || input.Nome.Trim().Length > 100
            || input.Curso?.Trim().Length > 150
            || input.Instituicao?.Trim().Length > 150)
        {
            return BadRequest(new
            {
                erro = "Dados de perfil inválidos."
            });
        }

        var utilizador = await db.Utilizadores
            .FirstOrDefaultAsync(u => u.Id == id.Value, ct);

        if (utilizador is null)
            return NotFound(new
            {
                erro = "Utilizador não encontrado."
            });

        utilizador.Nome = input.Nome.Trim();
        utilizador.Curso = string.IsNullOrWhiteSpace(input.Curso)
            ? null
            : input.Curso.Trim();
        utilizador.Instituicao =
            string.IsNullOrWhiteSpace(input.Instituicao)
                ? null
                : input.Instituicao.Trim();

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            utilizador.Id,
            utilizador.Nome,
            utilizador.Email,
            utilizador.EmailConfirmado,
            utilizador.Curso,
            utilizador.Instituicao
        });
    }
}
