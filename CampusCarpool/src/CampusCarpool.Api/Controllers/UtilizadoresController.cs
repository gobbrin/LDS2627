
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

/// <summary>Perfil do utilizador autenticado e perfil público de outros utilizadores (F001). Rotas em <c>/api/v1/utilizadores</c>.</summary>
/// <remarks>Usa o <see cref="CampusCarpoolDbContext"/> diretamente.</remarks>
[ApiController]
[Authorize]
[Route("api/v1/utilizadores")]
public sealed class UtilizadoresController(
    CampusCarpoolDbContext db) : ControllerBase
{
    // P-27: número máximo de ligações externas por utilizador.
    private const int MaximoLigacoes = 5;
    private const int TamanhoMaximoUrl = 2048;
    private const int TamanhoMaximoDescricao = 100;

    /// <summary>Corpo de <c>POST me/ligacoes</c>: URL absoluto http ou https e descrição opcional.</summary>
    public sealed record NovaLigacao(string Url, string? Descricao);

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
    /// <summary>Perfil do próprio utilizador, incluindo o email e as ligações externas (RF-F001-20). 200; 401; 404.</summary>
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

        var ligacoes = await ListarLigacoes(id.Value, ct);

        return Ok(new
        {
            perfil.Id,
            perfil.Nome,
            perfil.Email,
            perfil.EmailConfirmado,
            perfil.Curso,
            perfil.Instituicao,
            perfil.CriadoEm,
            LigacoesExternas = ligacoes
        });
    }

    private Task<List<LigacaoPerfil>> ListarLigacoes(Guid utilizadorId, CancellationToken ct) =>
        db.LigacoesExternas
            .AsNoTracking()
            .Where(l => l.UtilizadorId == utilizadorId)
            .OrderBy(l => l.Ordem)
            .Select(l => new LigacaoPerfil(l.Id, l.Url, l.Descricao))
            .ToListAsync(ct);

    /// <summary>Ligação a um perfil externo, tal como aparece nos perfis.</summary>
    public sealed record LigacaoPerfil(Guid Id, string Url, string? Descricao);

    /// <summary>Associa uma ligação a um perfil externo ao próprio perfil (RF-F001-15, RF-F001-16, P-27, DEF-BE-005).</summary>
    /// <returns>201 ligação criada; 400 URL que não seja absoluto http ou https, texto longo ou mais de 5 ligações; 401.</returns>
    [HttpPost("me/ligacoes")]
    public async Task<IActionResult> AdicionarLigacao([FromBody] NovaLigacao input, CancellationToken ct)
    {
        var id = ObterUtilizadorId();

        if (id is null)
            return Unauthorized();

        // RF-F001-16: recusa, por exemplo, javascript:alert(1) ou texto sem esquema.
        var urlValido = input.Url?.Length <= TamanhoMaximoUrl
            && Uri.TryCreate(input.Url.Trim(), UriKind.Absolute, out var url)
            && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);

        if (!urlValido || input.Descricao?.Trim().Length > TamanhoMaximoDescricao)
            return BadRequest(new { erro = "Ligação inválida: indica um URL http ou https." });

        var existentes = await db.LigacoesExternas
            .Where(l => l.UtilizadorId == id.Value)
            .Select(l => l.Ordem)
            .ToListAsync(ct);

        if (existentes.Count >= MaximoLigacoes)
            return BadRequest(new { erro = "O perfil já tem o número máximo de ligações." });

        var ligacao = new LigacaoExterna
        {
            UtilizadorId = id.Value,
            Url = input.Url!.Trim(),
            Descricao = string.IsNullOrWhiteSpace(input.Descricao) ? null : input.Descricao.Trim(),
            Ordem = existentes.Count == 0 ? 1 : existentes.Max() + 1
        };

        db.LigacoesExternas.Add(ligacao);
        await db.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new LigacaoPerfil(ligacao.Id, ligacao.Url, ligacao.Descricao));
    }

    /// <summary>Remove uma ligação do próprio perfil (RF-F001-22). 204; 401; 404 inexistente ou de outro utilizador.</summary>
    [HttpDelete("me/ligacoes/{ligacaoId:guid}")]
    public async Task<IActionResult> RemoverLigacao(Guid ligacaoId, CancellationToken ct)
    {
        var id = ObterUtilizadorId();

        if (id is null)
            return Unauthorized();

        var ligacao = await db.LigacoesExternas
            .FirstOrDefaultAsync(l => l.Id == ligacaoId && l.UtilizadorId == id.Value, ct);

        if (ligacao is null)
            return NotFound(new { erro = "Ligação não encontrada." });

        db.LigacoesExternas.Remove(ligacao);
        await db.SaveChangesAsync(ct);

        return NoContent();
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

        var ligacoes = await ListarLigacoes(id, ct);

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
    /// Tamanhos máximos de P-26: nome 100, curso e instituição 150 caracteres.
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
