using System.Security.Claims;
using CampusCarpool.Application.Participacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Simulação do percurso de uma viagem se o condutor aceitar um pedido pendente (RF-F004-09, RF-F008-06).
/// Rota <c>POST /api/v1/viagens/{viagemId}/simulacao</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/viagens/{viagemId:guid}/simulacao")]
public sealed class SimulacoesController(IParticipacaoService participacoes) : ControllerBase
{
    /// <summary>Corpo do pedido: o pedido pendente a simular.</summary>
    public sealed record SimularPedido(Guid PedidoId);

    /// <summary>Simula a aceitação do pedido indicado.</summary>
    /// <remarks>Ver <see cref="IParticipacaoService.SimularAsync"/>.</remarks>
    /// <returns>
    /// 200 simulação; 400 pedido sem pontos, ou recolha igual ao destino; 401; 403 não é o condutor; 404 viagem ou
    /// pedido inexistente; 409 viagem não ativa ou pedido não pendente; 422 local não encontrado;
    /// 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Simular(Guid viagemId, [FromBody] SimularPedido? input, CancellationToken ct)
    {
        var textoId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(textoId, out var utilizadorId)) return Unauthorized();
        if (input is null || input.PedidoId == Guid.Empty)
            return BadRequest(new { erro = "Indica um pedidoId válido." });

        return this.Responder(
            await participacoes.SimularAsync(viagemId, input.PedidoId, utilizadorId, ct),
            simulacao => Ok(simulacao));
    }
}
