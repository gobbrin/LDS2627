using CampusCarpool.Application.Comunicacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Envio e consulta de mensagens individuais (F005). Rotas em <c>/api/v1/mensagens</c>.</summary>
/// <remarks>O remetente e o leitor da conversa são sempre o utilizador autenticado.</remarks>
[ApiController]
[Authorize]
[Route("api/v1/mensagens")]
public sealed class MensagensController(IMensagemService mensagemService) : ControllerBase
{
    /// <summary>Corpo de <c>POST</c>: o destinatário e o texto.</summary>
    public sealed record EnviarMensagem(Guid DestinatarioId, string Conteudo);

    /// <summary>Envia uma mensagem (RF-F005-01, RF-F005-02). 201 com a mensagem; 400; 401.</summary>
    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] EnviarMensagem pedido, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid remetenteId) return Unauthorized();

        return this.Responder(
            await mensagemService.EnviarAsync(remetenteId, pedido.DestinatarioId, pedido.Conteudo, ct),
            mensagem => CreatedAtAction(nameof(ObterConversa), new { outroUtilizadorId = pedido.DestinatarioId }, mensagem));
    }

    /// <summary>Conversas do utilizador autenticado, da mais recente para a mais antiga (RF-F005-08, DEF-BE-018). 200; 401.</summary>
    [HttpGet("conversas")]
    public async Task<IActionResult> ListarConversas(CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return Ok(await mensagemService.ListarConversasAsync(utilizadorId, ct));
    }

    /// <summary>Conversa com outro utilizador, por ordem cronológica (RF-F005-03, RF-F005-09). 200; 400; 401.</summary>
    [HttpGet("conversa/{outroUtilizadorId:guid}")]
    public async Task<IActionResult> ObterConversa(Guid outroUtilizadorId, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(
            await mensagemService.ObterConversaAsync(utilizadorId, outroUtilizadorId, ct),
            mensagens => Ok(mensagens));
    }
}
