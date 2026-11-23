using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Disponibiliza as notificações pessoais do utilizador autenticado.</summary>
/// <remarks>Caixa de notificações (RF-F006-07). Rotas em <c>/api/v1/notificacoes</c>; todas exigem sessão (401 sem ela).</remarks>
[ApiController, Authorize, Route("api/v1/notificacoes")]
public sealed class NotificacoesController(INotificacaoService notificacaoService) : ControllerBase
{
    /// <summary>Lista notificações paginadas da mais recente para a mais antiga.</summary>
    /// <returns>200 <see cref="PaginaNotificacoes"/>; 400 paginação inválida.</returns>
    [HttpGet]
    /// <remarks>O tamanho chama-se <c>tamanhoPagina</c> por ser anterior à convenção <c>tamanho</c> (BE-34); mantém-se.</remarks>
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = Paginacao.TamanhoPorOmissao,
        CancellationToken ct = default)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        // A paginação valida-se só aqui (BE-41).
        if (Paginacao.Validar(pagina, tamanhoPagina) is { } paginacaoInvalida)
            return this.RespostaDeErro(paginacaoInvalida);

        return Ok(await notificacaoService.ListarAsync(utilizadorId, pagina, tamanhoPagina, ct));
    }

    /// <summary>Marca uma notificação do próprio utilizador como lida.</summary>
    /// <returns>204; 404 se não existir ou for de outro utilizador.</returns>
    [HttpPut("{id:guid}/ler")]
    public async Task<IActionResult> MarcarComoLida(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(await notificacaoService.MarcarComoLidaAsync(id, utilizadorId, ct), () => NoContent());
    }

    /// <summary>Marca todas as notificações do próprio utilizador como lidas.</summary>
    /// <returns>200 <c>{ marcadas }</c>.</returns>
    [HttpPut("ler-todas")]
    public async Task<IActionResult> MarcarTodasComoLidas(CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();
        var quantidade = await notificacaoService.MarcarTodasComoLidasAsync(utilizadorId, ct);
        return Ok(new NotificacoesMarcadas(quantidade));
    }
}
