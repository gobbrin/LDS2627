using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Api.Controllers;

/// <summary>Pesquisa de viagens (F003). Rota <c>GET /api/v1/pesquisa/viagens</c>.</summary>
/// <remarks>Usa o <see cref="CampusCarpoolDbContext"/> diretamente.</remarks>
[ApiController, Authorize, Route("api/v1/pesquisa")]
public sealed class PesquisaController(CampusCarpoolDbContext db) : ControllerBase
{
    /// <summary>Pesquisa viagens ativas, futuras e com lugares (RF-F003-01, RF-F003-02, RF-F003-05, P-12).</summary>
    /// <remarks>
    /// Filtros opcionais e combináveis: <c>origem</c> e <c>destino</c> por texto contido (sem distinção de
    /// maiúsculas no SQL Server; com distinção em SQLite, nos testes), <c>data</c> (dia) e <c>hora</c> (a partir
    /// de). Ordenado por partida crescente, paginado (<c>tamanho</c> até 100).
    /// DC-42 / API-06 (fica para m4): o dia e a hora são comparados em UTC, não na hora de Portugal; perto da
    /// meia-noite ou com hora de verão, uma viagem pode cair no dia ou hora errados.
    /// DEF-BE-011 (fica para m4): os resultados não trazem o nome nem a média do condutor (RF-F003-04).
    /// </remarks>
    /// <returns>200 <c>{ total, pagina, tamanho, viagens }</c>; 400 paginação inválida; 401.</returns>
    [HttpGet("viagens")]
    public async Task<IActionResult> Pesquisar([FromQuery] string? origem, [FromQuery] string? destino,
        [FromQuery] DateOnly? data, [FromQuery] TimeOnly? hora, [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || tamanho is < 1 or > 100 || (long)(pagina - 1) * tamanho > int.MaxValue) return BadRequest(new { erro = "Paginação inválida." });
        var q = db.Viagens.AsNoTracking().Where(v => v.Estado == nameof(EstadoViagem.Ativa) && v.LugaresDisponiveis > 0 && v.PartidaEm > DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(origem)) q = q.Where(v => v.Origem.Contains(origem.Trim()));
        if (!string.IsNullOrWhiteSpace(destino)) q = q.Where(v => v.Destino.Contains(destino.Trim()));
        if (data.HasValue) { var inicio = data.Value.ToDateTime(TimeOnly.MinValue); var fim = inicio.AddDays(1); q = q.Where(v => v.PartidaEm >= inicio && v.PartidaEm < fim); }
        if (hora.HasValue) { var start = hora.Value.Hour * 60 + hora.Value.Minute; q = q.Where(v => v.PartidaEm.Hour * 60 + v.PartidaEm.Minute >= start); }
        var total = await q.CountAsync(ct);
        var viagens = await q.OrderBy(v => v.PartidaEm).ThenBy(v => v.Id).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(v => new { v.Id, v.Origem, v.Destino, v.PartidaEm, v.LugaresDisponiveis, v.DistanciaKm, v.DuracaoMinutos }).ToListAsync(ct);
        return Ok(new { total, pagina, tamanho, viagens });
    }
}
