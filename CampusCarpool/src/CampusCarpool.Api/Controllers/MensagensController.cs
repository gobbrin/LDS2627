using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

//Disponibiliza os endpoints para envio e consulta de mensagens individuais.
[ApiController]
[Authorize]
[Route("api/v1/mensagens")]
public sealed class MensagensController(IMensagemService mensagemService) : ControllerBase {
    //Obtém o identificador do utilizador autenticado através do JWT.
    private Guid? UtilizadorId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : null;

    //Envia uma mensagem ao destinatário indicado no pedido.
    [HttpPost]
    public async Task<ActionResult> Enviar([FromBody] EnviarMensagemRequest pedido, CancellationToken ct) {
        //Impede operações sem um identificador de utilizador válido.
        if(UtilizadorId is not Guid remetenteId) {
            return Unauthorized();
        }

        try {
            //O remetente é sempre o utilizador autenticado.
            var mensagem = await mensagemService.EnviarAsync(remetenteId, pedido.DestinatarioId, pedido.Conteudo, ct);

            return CreatedAtAction(nameof(ObterConversa), new {outroUtilizadorId = pedido.DestinatarioId}, mensagem);
        } catch(ArgumentException ex) {
            //Devolve um erro de validação quando os dados são inválidos.
            return BadRequest(new {erro = ex.Message});
        } catch(InvalidOperationException ex) {
            //Indica que não foi possível executar a operação solicitada.
            return BadRequest(new {erro = ex.Message});
        }
    }

    //Obtém o histórico entre o utilizador autenticado e outro utilizador.
    [HttpGet("conversa/{outroUtilizadorId:guid}")]
    public async Task<IActionResult> ObterConversa(Guid outroUtilizadorId, CancellationToken ct) {
        //Impede operações sem um identificador de utilizador válido.
        if(UtilizadorId is not Guid utilizadorId) {
            return Unauthorized();
        }

        try {
            //A conversa é sempre consultada em relação ao utilizador autenticado.
            IReadOnlyList<Mensagem> mensagens = await mensagemService.ObterConversaAsync(utilizadorId, outroUtilizadorId, ct);

            return Ok(mensagens);
        } catch(ArgumentException ex) {
            return BadRequest(new {erro = ex.Message});
        } catch(InvalidOperationException ex) {
            return BadRequest(new {erro = ex.Message});
        }
    }
}

//Dados necessários para enviar uma mensagem individual.
public sealed record EnviarMensagemRequest(Guid DestinatarioId, string Conteudo);