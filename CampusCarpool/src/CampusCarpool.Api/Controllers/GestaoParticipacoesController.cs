using CampusCarpool.Application.Participacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Decisões sobre uma participação: aceitar, rejeitar e cancelar (F004).</summary>
/// <remarks>
/// As regras estão no <see cref="IParticipacaoService"/>. Rotas em <c>/api/v1/participacoes/{id}</c>; respostas: 204
/// sucesso, 401 sem sessão, 403 sem permissão, 404 inexistente, 409 estado não permite, sem lugares ou lugares
/// inconsistentes, 503 ao aceitar sem o Serviço de Mapas (P-19).
/// </remarks>
[ApiController, Authorize, Route("api/v1/participacoes")]
public sealed class GestaoParticipacoesController(IParticipacaoService participacaoService) : ControllerBase
{
    /// <summary>Aceita um pedido pendente quando o utilizador autenticado é o condutor.</summary>
    [HttpPost("{id:guid}/aceitar")]
    public async Task<IActionResult> Aceitar(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(await participacaoService.AceitarAsync(id, utilizadorId, ct), () => NoContent());
    }

    /// <summary>Rejeita um pedido pendente quando o utilizador autenticado é o condutor.</summary>
    [HttpPost("{id:guid}/rejeitar")]
    public async Task<IActionResult> Rejeitar(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(await participacaoService.RejeitarAsync(id, utilizadorId, ct), () => NoContent());
    }

    /// <summary>Cancela uma participação pendente ou aceite dentro das regras temporais.</summary>
    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(await participacaoService.CancelarAsync(id, utilizadorId, ct), () => NoContent());
    }
}
