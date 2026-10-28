using CampusCarpool.Api.Services;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusCarpool.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rotas")]
public sealed class RotasController(
    MapasService maps,
    EstimativaCustoService custos,
    CampusCarpoolDbContext? db = null,
    ConsultaRotaViagemService? consultaRotas = null) : ControllerBase
{
    public sealed record Pedido(string Origem, string Destino);

    [HttpGet("geocodificar")]
    public async Task<IActionResult> Geocodificar([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto)) return BadRequest();
        if (!maps.Configurado)
            return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        try
        {
            var resultado = await maps.GeocodificarAsync(texto, ct);
            return resultado is null ? NotFound() : Ok(resultado);
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { erro = "Falha no serviço externo de mapas." });
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { erro = "Tempo de resposta dos mapas excedido." });
        }
    }

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(Pedido input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Origem) || string.IsNullOrWhiteSpace(input.Destino))
            return BadRequest();

        if (!maps.Configurado)
            return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        try
        {
            var origem = await maps.GeocodificarAsync(input.Origem, ct);
            var destino = await maps.GeocodificarAsync(input.Destino, ct);

            if (origem is null || destino is null)
                return UnprocessableEntity(new { erro = "Localização não encontrada." });

            var rota = await maps.CalcularAsync(origem, destino, ct);
            return rota is null
                ? UnprocessableEntity(new { erro = "Rota indisponível." })
                : Ok(rota);
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { erro = "Falha no serviço externo de mapas." });
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { erro = "Tempo de resposta dos mapas excedido." });
        }
    }

    [HttpGet("viagens/{viagemId:guid}")]
    public async Task<IActionResult> ConsultarRotaViagem(Guid viagemId, CancellationToken ct)
    {
        if (db is null)
            return StatusCode(503, new { erro = "Base de dados indisponível." });

        var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idTexto, out var utilizadorId))
            return Unauthorized();

        var viagem = await db.Viagens
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == viagemId, ct);

        if (viagem is null)
            return NotFound(new { erro = "Viagem não encontrada." });

        var participacoes = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Aceite)
            .ToListAsync(ct);

        // Apenas o condutor ou um passageiro aceite pode ver os pontos detalhados.
        var autorizado = viagem.CondutorId == utilizadorId ||
            participacoes.Any(p => p.PassageiroId == utilizadorId);

        if (!autorizado)
            return Forbid();

        if (!maps.Configurado)
            return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        try
        {
            // Usar o mesmo cálculo de rota que será reutilizado na consulta da viagem.
            // Fallback para preservar os testes que instanciam este controlador diretamente.
            var servicoRotas = consultaRotas ?? new ConsultaRotaViagemService(maps);
            var resultado = await servicoRotas.CalcularAsync(viagem, participacoes, ct);

            if (resultado is null)
                return UnprocessableEntity(new { erro = "Não foi possível calcular a rota." });

            if (resultado.DistanciaTotalKm <= 0)
                return UnprocessableEntity(new
                {
                    erro = "Não existem dados suficientes para estimar o custo."
                });

            var estimativa = custos.Calcular(resultado.DistanciaTotalKm, participacoes.Count);

            return Ok(new
            {
                viagemId = viagem.Id,
                participacoesAceites = participacoes.Count,
                resultado.PontosOrdenados,
                resultado.Segmentos,
                resultado.DistanciaTotalKm,
                resultado.DuracaoTotalMinutos,
                EstimativaCusto = estimativa
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { erro = "Serviço de mapas indisponível." });
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { erro = "Tempo de resposta do serviço de mapas excedido." });
        }
    }
}
