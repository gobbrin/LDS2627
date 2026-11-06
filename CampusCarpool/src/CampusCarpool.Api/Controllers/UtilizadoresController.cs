using CampusCarpool.Application.Identidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Perfil do utilizador autenticado e perfil público de outros utilizadores (F001). Rotas em <c>/api/v1/utilizadores</c>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/utilizadores")]
public sealed class UtilizadoresController(IPerfilService perfis) : ControllerBase
{
    /// <summary>Corpo de <c>POST me/ligacoes</c>: URL absoluto http ou https e descrição opcional.</summary>
    public sealed record NovaLigacao(string Url, string? Descricao);

    /// <summary>Corpo de <c>PUT me</c>. O nome é obrigatório; curso e instituição vazios passam a <c>null</c>.</summary>
    public sealed record AtualizarPerfil(
        string Nome,
        string? Curso,
        string? Instituicao
    );

    // GET /api/v1/utilizadores/me
    /// <summary>Perfil do próprio utilizador, incluindo o email e as ligações externas (RF-F001-20). 200; 401; 404.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> ConsultarPerfil(
        CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id)
            return Unauthorized();

        return this.Responder(await perfis.ConsultarAsync(id, ct), perfil => Ok(perfil));
    }

    /// <summary>Associa uma ligação a um perfil externo ao próprio perfil (RF-F001-15, RF-F001-16, P-27, DEF-BE-005).</summary>
    /// <returns>201 ligação criada; 400 URL que não seja absoluto http ou https, texto longo ou mais de 5 ligações; 401.</returns>
    [HttpPost("me/ligacoes")]
    public async Task<IActionResult> AdicionarLigacao([FromBody] NovaLigacao input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id)
            return Unauthorized();

        return this.Responder(
            await perfis.AdicionarLigacaoAsync(id, input.Url, input.Descricao, ct),
            ligacao => StatusCode(StatusCodes.Status201Created, ligacao));
    }

    /// <summary>Remove uma ligação do próprio perfil (RF-F001-22). 204; 401; 404 inexistente ou de outro utilizador.</summary>
    [HttpDelete("me/ligacoes/{ligacaoId:guid}")]
    public async Task<IActionResult> RemoverLigacao(Guid ligacaoId, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id)
            return Unauthorized();

        return this.Responder(await perfis.RemoverLigacaoAsync(id, ligacaoId, ct), () => NoContent());
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
        CancellationToken ct)
    {
        if (User.IdUtilizador() is null)
            return Unauthorized();

        return this.Responder(await perfis.ConsultarPublicoAsync(id, ct), perfil => Ok(perfil));
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
        if (User.IdUtilizador() is not Guid id)
            return Unauthorized();

        return this.Responder(
            await perfis.AtualizarAsync(id, input.Nome, input.Curso, input.Instituicao, ct),
            perfil => Ok(perfil));
    }
}
