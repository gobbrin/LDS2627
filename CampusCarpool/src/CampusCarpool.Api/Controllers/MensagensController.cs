using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Disponibiliza os endpoints para envio e consulta de mensagens individuais.</summary>
/// <remarks>
/// F005, rotas em <c>/api/v1/mensagens</c>. Os erros do <see cref="IMensagemService"/> (argumento inválido ou
/// utilizador inexistente/por confirmar) dão sempre 400 (DO-09). A resposta devolve a entidade
/// <see cref="Mensagem"/> tal como está (inclui os ids do remetente e do destinatário).
/// Falta a lista das conversas do utilizador (RF-F005-08; DEF-BE-018, fica para m4).
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/mensagens")]
public sealed class MensagensController(IMensagemService mensagemService) : ControllerBase
{
    //Obtém o identificador do utilizador autenticado através do JWT.
    private Guid? UtilizadorId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : null;

    //Envia uma mensagem ao destinatário indicado no pedido.
    /// <summary>Envia uma mensagem (RF-F005-01, RF-F005-02). 201 com a mensagem; 400; 401.</summary>
    [HttpPost]
    public async Task<ActionResult> Enviar([FromBody] EnviarMensagemRequest pedido, CancellationToken ct)
    {
        //Impede operações sem um identificador de utilizador válido.
        if (UtilizadorId is not Guid remetenteId)
        {
            return Unauthorized();
        }

        try
        {
            //O remetente é sempre o utilizador autenticado.
            var mensagem = await mensagemService.EnviarAsync(remetenteId, pedido.DestinatarioId, pedido.Conteudo, ct);

            return CreatedAtAction(nameof(ObterConversa), new { outroUtilizadorId = pedido.DestinatarioId }, mensagem);
        }
        catch (ArgumentException ex)
        {
            //Devolve um erro de validação quando os dados são inválidos.
            return BadRequest(new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            //Indica que não foi possível executar a operação solicitada.
            return BadRequest(new { erro = ex.Message });
        }
    }

    //Obtém o histórico entre o utilizador autenticado e outro utilizador.
    /// <summary>Conversa com outro utilizador, por ordem cronológica (RF-F005-03, RF-F005-09). 200; 400; 401.</summary>
    [HttpGet("conversa/{outroUtilizadorId:guid}")]
    public async Task<IActionResult> ObterConversa(Guid outroUtilizadorId, CancellationToken ct)
    {
        //Impede operações sem um identificador de utilizador válido.
        if (UtilizadorId is not Guid utilizadorId)
        {
            return Unauthorized();
        }

        try
        {
            //A conversa é sempre consultada em relação ao utilizador autenticado.
            IReadOnlyList<Mensagem> mensagens = await mensagemService.ObterConversaAsync(utilizadorId, outroUtilizadorId, ct);

            return Ok(mensagens);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}

/// <summary>Dados necessários para enviar uma mensagem individual.</summary>
public sealed record EnviarMensagemRequest(Guid DestinatarioId, string Conteudo);
