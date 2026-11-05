using System.Security.Claims;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Simulação do percurso de uma viagem se o condutor aceitar um pedido pendente (RF-F004-09, RF-F008-06).
/// Rota <c>POST /api/v1/viagens/{viagemId}/simulacao</c>.
/// </summary>
/// <remarks>
/// Não altera nada. Compara a rota guardada com a rota que resultaria de juntar o pedido, calculada pelo mesmo
/// algoritmo (<see cref="OtimizadorRotas"/>, P-07), por isso a simulação coincide com a rota depois de aceitar.
/// Pontos omitidos assumem a origem ou o destino da viagem (P-20). Todo o cálculo tem um limite de 10 segundos.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/viagens/{viagemId:guid}/simulacao")]
public sealed class SimulacoesController(
    CampusCarpoolDbContext db,
    IServicoMapasRotas mapas,
    RotaViagemService? rotaViagem = null) : ControllerBase
{
    /// <summary>Corpo do pedido: o pedido pendente a simular.</summary>
    public sealed record SimularPedido(Guid PedidoId);
    /// <summary>Resultado: rota original e simulada (distância em km, duração em minutos, ordem dos pontos) e o desvio.</summary>
    public sealed record RespostaSimulacao(
        Guid ViagemId, Guid PedidoId, string OrigemViagem, string DestinoViagem,
        string RecolhaPassageiro, string DestinoPassageiro,
        decimal DistanciaOriginalKm, int DuracaoOriginalMinutos,
        decimal DistanciaSimuladaKm, int DuracaoSimuladaMinutos,
        decimal DesvioDistanciaKm, int DesvioDuracaoMinutos,
        IReadOnlyList<string> OrdemOriginal, IReadOnlyList<string> OrdemSimulada);

    /// <summary>Simula a aceitação do pedido indicado.</summary>
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

        var viagem = await db.Viagens.AsNoTracking().FirstOrDefaultAsync(v => v.Id == viagemId, ct);
        if (viagem is null) return NotFound(new { erro = "Viagem não encontrada." });
        if (viagem.CondutorId != utilizadorId) return Forbid();
        // A simulação antecede a decisão (RF-F004-09), por isso segue a mesma regra da aceitação (RF-F004-10).
        if (viagem.Estado != nameof(EstadoViagem.Ativa))
            return Conflict(new { erro = "A viagem não está ativa." });

        var pedido = await db.Participacoes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == input.PedidoId && p.ViagemId == viagemId, ct);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado nesta viagem." });
        if (pedido.Estado != EstadoParticipacao.Pendente)
            return Conflict(new { erro = "Só é possível simular pedidos pendentes." });
        if (string.IsNullOrWhiteSpace(pedido.Recolha) && string.IsNullOrWhiteSpace(pedido.Destino))
            return BadRequest(new { erro = "O pedido deve indicar recolha ou destino." });
        if (!mapas.Configurado) return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        var aceites = await db.Participacoes.AsNoTracking()
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Aceite)
            .ToListAsync(ct);

        var recolha = string.IsNullOrWhiteSpace(pedido.Recolha) ? viagem.Origem : pedido.Recolha.Trim();
        var destino = string.IsNullOrWhiteSpace(pedido.Destino) ? viagem.Destino : pedido.Destino.Trim();
        if (string.Equals(recolha, destino, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { erro = "A recolha e o destino efetivos não podem ser iguais." });

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            // A rota atual é a guardada; só falta calculá-la se estiver por recalcular (P-19). Nada é gravado.
            var servicoRotas = rotaViagem ?? new RotaViagemService(mapas);
            var original = RotaViagemService.Ler(viagem) ?? await servicoRotas.CalcularAsync(viagem, aceites, limite.Token);
            var simulada = await servicoRotas.CalcularAsync(viagem, [.. aceites, pedido], limite.Token);

            if (original is null || simulada is null)
                return UnprocessableEntity(new { erro = "Não foi possível calcular a rota." });

            return Ok(new RespostaSimulacao(
                viagem.Id, pedido.Id, viagem.Origem, viagem.Destino, recolha, destino,
                original.DistanciaTotalKm, original.DuracaoTotalMinutos,
                simulada.DistanciaTotalKm, simulada.DuracaoTotalMinutos,
                Math.Round(simulada.DistanciaTotalKm - original.DistanciaTotalKm, 2),
                simulada.DuracaoTotalMinutos - original.DuracaoTotalMinutos,
                original.PontosOrdenados.Select(p => p.Local).ToList(),
                simulada.PontosOrdenados.Select(p => p.Local).ToList()));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { erro = "Falha na comunicação com o serviço de mapas." }); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(504, new { erro = "O serviço de mapas excedeu o limite de 10 segundos." }); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(504, new { erro = "O serviço de mapas excedeu o limite de 10 segundos." }); }
    }
}
