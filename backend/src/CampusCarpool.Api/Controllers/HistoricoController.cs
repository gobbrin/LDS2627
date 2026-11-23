using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Historico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Histórico pessoal do utilizador autenticado (F009). Rota <c>GET /api/v1/historico</c>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/historico")]
public sealed class HistoricoController(
    IHistoricoService historicoService) : ControllerBase
{
    /// <summary>
    /// Devolve uma página do <see cref="HistoricoResultado"/> do próprio utilizador (RF-F009-01 a RF-F009-08, D-4):
    /// <c>GET /historico?pagina=1&amp;tamanho=20</c>; a página aplica-se a cada uma das seis listas.
    /// </summary>
    /// <remarks>Não recebe id: cada utilizador só vê o seu histórico (RF-F009-07).</remarks>
    /// <returns>200 com as listas, <c>pagina</c>, <c>tamanho</c> e <c>totais</c>; 400 paginação inválida; 401.</returns>
    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = Paginacao.TamanhoPorOmissao,
        CancellationToken ct = default)
    {
        if (User.IdUtilizador() is not Guid utilizadorId)
            return Unauthorized();

        if (Paginacao.Validar(pagina, tamanho) is { } paginacaoInvalida)
            return this.RespostaDeErro(paginacaoInvalida);

        var resultado = await historicoService.ConsultarAsync(utilizadorId, pagina, tamanho, ct);

        return Ok(resultado);
    }
}
