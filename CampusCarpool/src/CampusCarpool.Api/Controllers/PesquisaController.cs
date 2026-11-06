using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Viagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CampusCarpool.Api.Controllers;

/// <summary>Pesquisa de viagens (F003). Rota <c>GET /api/v1/pesquisa/viagens</c>.</summary>
[ApiController, Authorize, Route("api/v1/pesquisa")]
public sealed class PesquisaController(IPesquisaService pesquisa) : ControllerBase
{
    /// <summary>Pesquisa viagens ativas, futuras e com lugares (RF-F003-01, RF-F003-02, RF-F003-05, P-12).</summary>
    /// <remarks>
    /// Filtros opcionais e combináveis: <c>origem</c> e <c>destino</c> por texto contido, sem distinguir maiúsculas
    /// nem acentos, <c>data</c> (dia) e <c>hora</c> (a partir de). Ordenado por partida crescente, paginado
    /// (<c>tamanho</c> até 100).
    /// </remarks>
    /// <returns>200 <c>{ total, pagina, tamanho, viagens }</c>; 400 paginação inválida; 401.</returns>
    [HttpGet("viagens")]
    public async Task<IActionResult> Pesquisar([FromQuery] string? origem, [FromQuery] string? destino,
        [FromQuery] DateOnly? data, [FromQuery] TimeOnly? hora, [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || tamanho is < 1 or > 100 || (long)(pagina - 1) * tamanho > int.MaxValue)
            return this.RespostaDeErro(new Erro(TipoErro.DadosInvalidos, "Paginação inválida."));

        return Ok(await pesquisa.PesquisarAsync(
            new FiltroPesquisaViagens(origem, destino, data, hora, pagina, tamanho), ct));
    }
}
