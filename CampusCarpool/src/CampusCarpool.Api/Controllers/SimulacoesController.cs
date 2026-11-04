using System.Security.Claims;
using CampusCarpool.Api.Services;
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
/// Não altera nada. Compara a rota atual (com os aceites) com a rota que resultaria de juntar o pedido, ambas
/// otimizadas pelo <see cref="OtimizadorRotas"/> (P-07). Pontos omitidos assumem a origem ou o destino da viagem
/// (P-20). Todo o cálculo tem um limite de 10 segundos.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/viagens/{viagemId:guid}/simulacao")]
public sealed class SimulacoesController(CampusCarpoolDbContext db, MapasService mapas) : ControllerBase
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
    /// pedido inexistente; 409 viagem não ativa/já partida ou pedido não pendente; 422 local não encontrado;
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
        if (viagem.Estado != nameof(EstadoViagem.Ativa) || viagem.PartidaEm <= DateTime.UtcNow)
            return Conflict(new { erro = "A viagem não está ativa ou já começou." });

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
            .OrderBy(p => p.Id).ToListAsync(ct);

        var recolha = string.IsNullOrWhiteSpace(pedido.Recolha) ? viagem.Origem : pedido.Recolha.Trim();
        var destino = string.IsNullOrWhiteSpace(pedido.Destino) ? viagem.Destino : pedido.Destino.Trim();
        if (string.Equals(recolha, destino, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { erro = "A recolha e o destino efetivos não podem ser iguais." });

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var passageiros = aceites.Select(p => new ParagemPassageiro(
                string.IsNullOrWhiteSpace(p.Recolha) ? viagem.Origem : p.Recolha.Trim(),
                string.IsNullOrWhiteSpace(p.Destino) ? viagem.Destino : p.Destino.Trim())).ToList();
            var original = await OtimizadorRotas.CalcularAsync(mapas, viagem.Origem, viagem.Destino, passageiros, limite.Token);
            passageiros.Add(new ParagemPassageiro(recolha, destino));
            var simulada = await OtimizadorRotas.CalcularAsync(mapas, viagem.Origem, viagem.Destino, passageiros, limite.Token);
            return Ok(new RespostaSimulacao(
                viagem.Id, pedido.Id, viagem.Origem, viagem.Destino, recolha, destino,
                original.DistanciaKm, original.DuracaoMinutos,
                simulada.DistanciaKm, simulada.DuracaoMinutos,
                Math.Round(simulada.DistanciaKm - original.DistanciaKm, 2),
                simulada.DuracaoMinutos - original.DuracaoMinutos,
                original.Ordem, simulada.Ordem));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { erro = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { erro = "Falha na comunicação com o serviço de mapas." }); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(504, new { erro = "O serviço de mapas excedeu o limite de 10 segundos." }); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(504, new { erro = "O serviço de mapas excedeu o limite de 10 segundos." }); }
    }
}
