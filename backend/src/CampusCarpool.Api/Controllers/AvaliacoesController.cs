using CampusCarpool.Application.Avaliacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Avaliações entre participantes (F007). Rotas em <c>/api/v1/avaliacoes</c>.</summary>
/// <remarks>As regras estão no <see cref="IAvaliacaoService"/>.</remarks>
[ApiController, Authorize, Route("api/v1/avaliacoes")]
public sealed class AvaliacoesController(IAvaliacaoService avaliacoes) : ControllerBase
{
    /// <summary>Corpo de <c>POST</c>: viagem concluída, utilizador avaliado e valor de 1 a 5.</summary>
    public sealed record CriarAvaliacao(Guid ViagemId, Guid AvaliadoId, int Classificacao);

    /// <summary>Corpo de <c>PUT</c>: novo valor de 1 a 5.</summary>
    public sealed record AlterarAvaliacao(int Classificacao);

    /// <summary>Cria uma avaliação (RF-F007-01 a RF-F007-04). 201; 400; 401; 404; 409.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarAvaliacao pedido, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id) return Unauthorized();

        return this.Responder(
            await avaliacoes.CriarAsync(pedido.ViagemId, id, pedido.AvaliadoId, pedido.Classificacao, ct),
            avaliacao => StatusCode(StatusCodes.Status201Created, avaliacao));
    }

    /// <summary>Altera a avaliação do utilizador autenticado sobre <paramref name="avaliadoId"/> (RF-F007-05). 200; 400; 401; 404; 409.</summary>
    [HttpPut("utilizadores/{avaliadoId:guid}")]
    public async Task<IActionResult> Alterar(Guid avaliadoId, [FromBody] AlterarAvaliacao pedido, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id) return Unauthorized();

        return this.Responder(
            await avaliacoes.AlterarAsync(id, avaliadoId, pedido.Classificacao, ct),
            avaliacao => Ok(avaliacao));
    }

    /// <summary>Avaliações recebidas por um utilizador, sem o autor, e a média (RF-F007-07, RF-F007-11). 200; 401; 404.</summary>
    [HttpGet("utilizadores/{utilizadorId:guid}")]
    public async Task<IActionResult> Consultar(Guid utilizadorId, CancellationToken ct)
    {
        if (User.IdUtilizador() is null) return Unauthorized();

        return this.Responder(await avaliacoes.ConsultarAsync(utilizadorId, ct), resumo => Ok(resumo));
    }
}
