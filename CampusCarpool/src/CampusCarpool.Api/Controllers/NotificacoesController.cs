using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Disponibiliza as notificações pessoais do utilizador autenticado.</summary>
[ApiController, Authorize, Route("api/v1/notificacoes")]
public sealed class NotificacoesController(INotificacaoService notificacaoService) : ControllerBase
{
	private Guid? UtilizadorId => Guid.TryParse(
		User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
		out var id) ? id : null;

	/// <summary>Lista notificações paginadas da mais recente para a mais antiga.</summary>
	[HttpGet]
	public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default)
	{
		if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
		if (pagina < 1 || tamanhoPagina is < 1 or > 100)
			return BadRequest(new { erro = "Parâmetros de paginação inválidos." });
		return Ok(await notificacaoService.ListarAsync(utilizadorId, pagina, tamanhoPagina, ct));
	}

	/// <summary>Marca uma notificação do próprio utilizador como lida.</summary>
	[HttpPut("{id:guid}/ler")]
	public async Task<IActionResult> MarcarComoLida(Guid id, CancellationToken ct)
	{
		if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
		return await notificacaoService.MarcarComoLidaAsync(id, utilizadorId, ct)
			? NoContent()
			: NotFound();
	}

	/// <summary>Marca todas as notificações do próprio utilizador como lidas.</summary>
	[HttpPut("ler-todas")]
	public async Task<IActionResult> MarcarTodasComoLidas(CancellationToken ct)
	{
		if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
		var quantidade = await notificacaoService.MarcarTodasComoLidasAsync(utilizadorId, ct);
		return Ok(new { marcadas = quantidade });
	}
}
