using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Participacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Simulações do percurso de uma viagem com um pedido, sem gravar nada (RF-F004-09, RF-F008-06):
/// <c>POST /api/v1/viagens/{viagemId}/simulacao</c> (o condutor, com um pedido pendente) e
/// <c>POST /api/v1/viagens/{viagemId}/simular-pedido</c> (o passageiro, antes de enviar o pedido).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/viagens/{viagemId:guid}")]
public sealed class SimulacoesController(ISimulacaoDePedidoService simulacoes) : ControllerBase
{
    /// <summary>Corpo do pedido: o pedido pendente a simular.</summary>
    public sealed record SimularPedido(Guid PedidoId);

    /// <summary>Corpo da simulação do passageiro: os pontos, como em <c>POST /pedidos</c> (P-20).</summary>
    public sealed record SimularNovoPedido(string? Recolha, string? Destino);

    /// <summary>Simula a aceitação do pedido indicado.</summary>
    /// <remarks>Ver <see cref="ISimulacaoDePedidoService.SimularAsync"/>. A resposta traz <c>rotaSimulada</c> para o mapa (BE-35).</remarks>
    /// <returns>
    /// 200 simulação; 400 pedido sem pontos, ou recolha igual ao destino; 401; 403 não é o condutor; 404 viagem ou
    /// pedido inexistente; 409 viagem não ativa ou pedido não pendente; 422 local não encontrado;
    /// 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPost("simulacao")]
    public async Task<IActionResult> Simular(Guid viagemId, [FromBody] SimularPedido? input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();
        if (input is null || input.PedidoId == Guid.Empty)
            return this.RespostaDeErro(new Erro(TipoErro.DadosInvalidos, "Indica um pedidoId válido."));

        return this.Responder(
            await simulacoes.SimularAsync(viagemId, input.PedidoId, utilizadorId, ct),
            simulacao => Ok(simulacao));
    }

    /// <summary>Simula, para o passageiro, a viagem com o pedido que ele ainda vai enviar (ecrã 14 do protótipo).</summary>
    /// <remarks>
    /// Ver <see cref="ISimulacaoDePedidoService.SimularNovoPedidoAsync"/>. Não cria o pedido. A <c>rotaSimulada</c> só
    /// tem a origem, o destino e os pontos indicados (P-13).
    /// </remarks>
    /// <returns>
    /// 200 simulação; 400 sem pontos, pontos vazios ou com mais de 200 caracteres, ou no mesmo local; 401; 403 é o
    /// condutor; 404 viagem inexistente; 409 viagem indisponível, sem lugares ou pedido repetido (RF-F004-06);
    /// 422 local não encontrado; 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPost("simular-pedido")]
    public async Task<IActionResult> SimularNovo(Guid viagemId, [FromBody] SimularNovoPedido? input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(
            await simulacoes.SimularNovoPedidoAsync(viagemId, utilizadorId, input?.Recolha, input?.Destino, ct),
            simulacao => Ok(simulacao));
    }
}
