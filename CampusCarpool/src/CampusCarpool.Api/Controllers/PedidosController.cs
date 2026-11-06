using CampusCarpool.Application.Participacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Submissão de pedidos de participação (F004). Rota <c>/api/v1/pedidos</c>.</summary>
/// <remarks>
/// Só cria o pedido; aceitar, rejeitar e cancelar estão no <c>GestaoParticipacoesController</c>. A lista dos pedidos
/// pendentes de uma viagem está em <c>GET /viagens/{id}/pedidos</c>.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/pedidos")]
public sealed class PedidosController(IParticipacaoService participacoes) : ControllerBase
{
    /// <summary>Corpo de <c>POST /pedidos</c>. Pontos omitidos (<c>null</c>) significam a origem/o destino da viagem.</summary>
    public sealed record CriarPedido(
        Guid ViagemId,
        string? Recolha,
        string? Destino);

    /// <summary>Submete um pedido de participação, no estado <c>Pendente</c> (RF-F004-01 a RF-F004-07, RF-F004-13).</summary>
    /// <remarks>Ver <see cref="IParticipacaoService.CriarPedidoAsync"/>.</remarks>
    /// <returns>
    /// 201 pedido criado; 400 sem nenhum ponto (P-20), pontos vazios ou com mais de 250 caracteres, ou pontos no
    /// mesmo local (RF-F004-18, RF-F004-20, RF-F004-21); 401; 403 conta por confirmar; 404 viagem
    /// inexistente; 409 viagem não ativa ou já partida, própria viagem (RF-F004-05), pedido repetido (RF-F004-06) ou
    /// sem lugares; 422 ponto não encontrado; 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Criar(CriarPedido input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid passageiroId)
            return Unauthorized();

        return this.Responder(
            await participacoes.CriarPedidoAsync(input.ViagemId, passageiroId, input.Recolha, input.Destino, ct),
            pedido => StatusCode(StatusCodes.Status201Created, pedido));
    }
}
