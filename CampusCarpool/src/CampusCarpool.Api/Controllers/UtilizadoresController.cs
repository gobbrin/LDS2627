
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
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

    /// <summary>
    /// Perfil público de um utilizador (RF-F001-17, RF-F001-19, RF-F007-10; DEF-BE-006).
    /// Devolve só os campos de P-10: nome, curso, instituição, ligações externas e
    /// classificação média (1 casa decimal; <c>null</c> com 0 avaliações, "sem avaliações").
    /// Nunca devolve o email. 200; 401 sem sessão; 404 se o utilizador não existir.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ConsultarPerfilPublico(
        Guid id,
        [FromServices] IAvaliacaoService avaliacoes,
        CancellationToken ct)
    {
        if (ObterUtilizadorId() is null)
            return Unauthorized();

        var utilizador = await db.Utilizadores
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.Nome, u.Curso, u.Instituicao })
            .FirstOrDefaultAsync(ct);

        if (utilizador is null)
            return NotFound(new
            {
                erro = "Utilizador não encontrado."
            });

        var ligacoes = await db.LigacoesExternas
            .AsNoTracking()
            .Where(l => l.UtilizadorId == id)
            .OrderBy(l => l.Ordem)
            .Select(l => new { l.Url, l.Descricao })
            .ToListAsync(ct);

        var resumo = await avaliacoes.ConsultarAsync(id, ct);

        return Ok(new
        {
            utilizador.Id,
            utilizador.Nome,
            utilizador.Curso,
            utilizador.Instituicao,
            LigacoesExternas = ligacoes,
            ClassificacaoMedia = resumo.Media,
            TotalAvaliacoes = resumo.Total
        });
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
