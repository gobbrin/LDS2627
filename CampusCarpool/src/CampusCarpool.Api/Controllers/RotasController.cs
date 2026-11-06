using CampusCarpool.Application.Rotas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Serviço de Mapas e Rotas exposto ao frontend (F008). Rotas em <c>/api/v1/rotas</c>.</summary>
/// <remarks>Todos os métodos respondem 503 se o serviço não estiver configurado, 502 se falhar e 504 se exceder o tempo (RF-F008-12).</remarks>
[ApiController]
[Authorize]
[Route("api/v1/rotas")]
public sealed class RotasController(IConsultaRotasService rotas) : ControllerBase
{
    /// <summary>Corpo de <c>POST calcular</c>: dois locais em texto.</summary>
    public sealed record Pedido(string Origem, string Destino);

    /// <summary>Converte um texto num par de coordenadas (autocompletar e validação no frontend).</summary>
    /// <returns>200 coordenadas; 400 texto vazio; 404 local não encontrado; 502/503/504.</returns>
    [HttpGet("geocodificar")]
    public async Task<IActionResult> Geocodificar([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto)) return BadRequest();

        return this.Responder(await rotas.GeocodificarAsync(texto, ct), coordenadas => Ok(coordenadas));
    }

    /// <summary>Calcula a rota direta entre dois locais, sem gravar nada (pré-visualização ao criar a viagem).</summary>
    /// <returns>200 rota (distância, duração, geometria); 400; 422 local ou rota não encontrados; 502/503/504.</returns>
    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(Pedido input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Origem) || string.IsNullOrWhiteSpace(input.Destino))
            return BadRequest();

        return this.Responder(await rotas.CalcularAsync(input.Origem, input.Destino, ct), rota => Ok(rota));
    }

    /// <summary>
    /// Rota de uma viagem com os pontos das participações aceites, na ordem calculada, e a estimativa de custo
    /// (RF-F008-03 a RF-F008-05, RF-F008-10).
    /// </summary>
    /// <remarks>Ver <see cref="IConsultaRotasService.ConsultarDaViagemAsync"/>.</remarks>
    /// <returns>200; 401; 404 viagem inexistente ou que o utilizador não pode consultar; 422 rota não calculável; 502/503/504.</returns>
    [HttpGet("viagens/{viagemId:guid}")]
    public async Task<IActionResult> ConsultarRotaViagem(Guid viagemId, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId)
            return Unauthorized();

        return this.Responder(
            await rotas.ConsultarDaViagemAsync(viagemId, utilizadorId, ct),
            rota => Ok(new
            {
                viagemId = rota.ViagemId,
                participacoesAceites = rota.ParticipacoesAceites,
                rota.PontosOrdenados,
                rota.Segmentos,
                rota.DistanciaTotalKm,
                rota.DuracaoTotalMinutos,
                rota.EstimativaCusto
            }));
    }
}
