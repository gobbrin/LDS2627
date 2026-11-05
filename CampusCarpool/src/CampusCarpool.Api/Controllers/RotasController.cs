using System.Security.Claims;
using CampusCarpool.Api.Services;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

/// <summary>Serviço de Mapas e Rotas exposto ao frontend (F008). Rotas em <c>/api/v1/rotas</c>.</summary>
/// <remarks>
/// Todos os métodos dependem do <see cref="MapasService"/> (OpenRouteService) e respondem 503 se não estiver
/// configurado, 502 se falhar e 504 se exceder o tempo (RF-F008-12). Os parâmetros opcionais (<c>= null</c>)
/// existem para os testes que criam o controller sem DI.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/rotas")]
public sealed class RotasController(
    MapasService maps,
    EstimativaCustoService custos,
    CampusCarpoolDbContext? db = null,
    RotaViagemService? rotaViagem = null) : ControllerBase
{
    /// <summary>Corpo de <c>POST calcular</c>: dois locais em texto.</summary>
    public sealed record Pedido(string Origem, string Destino);

    // P-13: um só segmento da origem ao destino, com os totais, sem revelar por onde a rota passa.
    private static ResultadoRotaComPontos SemPontosDasParticipacoes(ResultadoRotaComPontos rota)
    {
        var origem = rota.PontosOrdenados[0];
        var destino = rota.PontosOrdenados[^1];

        return rota with
        {
            PontosOrdenados = [origem, destino],
            Segmentos = [new SegmentoRota(origem, destino, rota.DistanciaTotalKm, rota.DuracaoTotalMinutos)]
        };
    }

    /// <summary>Converte um texto num par de coordenadas (autocompletar e validação no frontend).</summary>
    /// <returns>200 coordenadas; 400 texto vazio; 404 local não encontrado; 502/503/504.</returns>
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

    /// <summary>Calcula a rota direta entre dois locais, sem gravar nada (pré-visualização ao criar a viagem).</summary>
    /// <returns>200 rota (distância, duração, geometria); 400; 422 local ou rota não encontrados; 502/503/504.</returns>
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

    /// <summary>
    /// Rota de uma viagem com os pontos das participações aceites, na ordem calculada, e a estimativa de custo
    /// (RF-F008-03 a RF-F008-05, RF-F008-10).
    /// </summary>
    /// <remarks>
    /// O condutor e os passageiros aceites veem a rota completa (P-13, RF-F008-09). Quem pode consultar a viagem vê
    /// a rota entre a origem e o destino, sem os pontos das participações (RF-F003-08, DEF-BE-013). Lê a rota guardada, sem chamar o Serviço de Mapas; uma rota
    /// por recalcular é recalculada aqui (P-19) e, se o serviço falhar, responde que a rota não está disponível.
    /// </remarks>
    /// <returns>200; 401; 404 viagem inexistente ou que o utilizador não pode consultar; 422 rota não calculável; 502/503/504.</returns>
    [HttpGet("viagens/{viagemId:guid}")]
    public async Task<IActionResult> ConsultarRotaViagem(Guid viagemId, CancellationToken ct)
    {
        if (db is null)
            return StatusCode(503, new { erro = "Base de dados indisponível." });

        var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idTexto, out var utilizadorId))
            return Unauthorized();

        // Seguida pelo contexto: uma rota por recalcular é gravada ao ser obtida.
        var viagem = await db.Viagens
            .FirstOrDefaultAsync(v => v.Id == viagemId, ct);

        if (viagem is null)
            return NotFound(new { erro = "Viagem não encontrada." });

        var participacoes = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Aceite)
            .ToListAsync(ct);

        if (!await ConsultaViagens.PodeConsultarAsync(db, viagem, utilizadorId, ct))
            return NotFound(new { erro = "Viagem não encontrada." });

        // Apenas o condutor ou um passageiro aceite pode ver os pontos detalhados.
        var veOsPontos = viagem.CondutorId == utilizadorId ||
            participacoes.Any(p => p.PassageiroId == utilizadorId);

        if (viagem.RotaPontos is null && !maps.Configurado)
            return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        try
        {
            var servicoRotas = rotaViagem ?? new RotaViagemService(maps);
            var resultado = await servicoRotas.ObterAsync(db, viagem, participacoes, ct);

            if (resultado is null)
                return UnprocessableEntity(new { erro = "Não foi possível calcular a rota." });

            if (resultado.DistanciaTotalKm <= 0)
                return UnprocessableEntity(new
                {
                    erro = "Não existem dados suficientes para estimar o custo."
                });

            var estimativa = custos.Calcular(resultado.DistanciaTotalKm, participacoes.Count);
            var rotaVisivel = veOsPontos ? resultado : SemPontosDasParticipacoes(resultado);

            return Ok(new
            {
                viagemId = viagem.Id,
                participacoesAceites = participacoes.Count,
                rotaVisivel.PontosOrdenados,
                rotaVisivel.Segmentos,
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
