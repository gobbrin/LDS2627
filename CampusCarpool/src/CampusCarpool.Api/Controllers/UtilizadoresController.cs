
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

/// <summary>Perfil do utilizador autenticado e perfil público de outros utilizadores (F001). Rotas em <c>/api/v1/utilizadores</c>.</summary>
/// <remarks>
/// Usa o <see cref="CampusCarpoolDbContext"/> diretamente. Não há endpoints para as ligações externas
/// (RF-F001-15, RF-F001-16, RF-F001-22; DEF-BE-005, fica para m4).
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/utilizadores")]
public sealed class UtilizadoresController(
    CampusCarpoolDbContext db) : ControllerBase
{
    /// <summary>Corpo de <c>PUT me</c>. O nome é obrigatório; curso e instituição vazios passam a <c>null</c>.</summary>
    public sealed record AtualizarPerfil(
        string Nome,
        string? Curso,
        string? Instituicao
    );

    // Id do utilizador autenticado, lido da claim "sub" (o JwtBearer mapeia-a para NameIdentifier).
    private Guid? ObterUtilizadorId()
    {
        var valor =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(valor, out var id) ? id : null;
    }

    // GET /api/v1/utilizadores/me
    /// <summary>Perfil do próprio utilizador, incluindo o email (RF-F001-20). 200; 401; 404.</summary>
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
    /// <summary>Altera nome, curso e instituição do próprio utilizador (RF-F001-14).</summary>
    /// <remarks>
    /// Só altera o perfil do utilizador do token, por isso ninguém altera o perfil de outro (RF-F001-18).
    /// O limite de 150 caracteres para curso e instituição é do código; P-26 ainda não o define (D-004).
    /// </remarks>
    /// <returns>200 perfil atualizado; 400 dados inválidos; 401; 404.</returns>
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
