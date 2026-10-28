using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Api.Controllers;

[ApiController, Authorize, Route("api/v1/pesquisa")]
public sealed class PesquisaController(CampusCarpoolDbContext db) : ControllerBase
{
    [HttpGet("viagens")]
    public async Task<IActionResult> Pesquisar([FromQuery] string? origem, [FromQuery] string? destino,
        [FromQuery] DateOnly? data, [FromQuery] TimeOnly? hora, [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || tamanho is < 1 or > 100 || (long)(pagina - 1) * tamanho > int.MaxValue) return BadRequest(new { erro = "Paginação inválida." });
        var q = db.Viagens.AsNoTracking().Where(v => v.Estado == "Ativa" && v.LugaresDisponiveis > 0 && v.PartidaEm > DateTime.UtcNow);
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
