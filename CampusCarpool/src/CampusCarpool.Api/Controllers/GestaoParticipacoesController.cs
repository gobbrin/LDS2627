using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Expõe as operações HTTP de gestão das participações de uma viagem.
/// </summary>
/// <remarks>
/// Este controller limita-se a autenticar o utilizador, chamar
/// <see cref="IParticipacaoService"/> e converter o resultado para HTTP.
/// As regras de negócio e o acesso a EF Core vivem em
/// <c>Infrastructure/Services/ParticipacaoService.cs</c>.
/// </remarks>
[ApiController, Authorize, Route("api/v1/participacoes")]
public sealed class GestaoParticipacoesController(IParticipacaoService participacaoService) : ControllerBase
{
    /// <summary>Obtém o identificador do utilizador autenticado a partir do claim JWT.</summary>
    private Guid? UtilizadorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;

    /// <summary>Converte o resultado da aplicação para a resposta HTTP correspondente.</summary>
    private IActionResult ConverterResultado(ResultadoGestaoParticipacao resultado) => resultado switch
    {
        ResultadoGestaoParticipacao.Sucesso => NoContent(),
        ResultadoGestaoParticipacao.NaoEncontrada => NotFound(),
        ResultadoGestaoParticipacao.NaoAutorizado => Forbid(),
        ResultadoGestaoParticipacao.SemCapacidade => Conflict(new { erro = "Sem lugares disponíveis." }),
        ResultadoGestaoParticipacao.DadosInconsistentes => Conflict(new { erro = "A capacidade da viagem está inconsistente." }),
        _ => Conflict()
    };

    [HttpPost("{id:guid}/aceitar")]
    /// <summary>Aceita um pedido pendente quando o utilizador autenticado é o condutor.</summary>
    public async Task<IActionResult> Aceitar(Guid id, CancellationToken ct)
    {
        if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
        return ConverterResultado(await participacaoService.AceitarAsync(id, utilizadorId, ct));
    }
    [HttpPost("{id:guid}/rejeitar")]
    /// <summary>Rejeita um pedido pendente quando o utilizador autenticado é o condutor.</summary>
    public async Task<IActionResult> Rejeitar(Guid id, CancellationToken ct)
    {
        if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
        return ConverterResultado(await participacaoService.RejeitarAsync(id, utilizadorId, ct));
    }
    [HttpPost("{id:guid}/cancelar")]
    /// <summary>Cancela uma participação pendente ou aceite dentro das regras temporais.</summary>
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        if (UtilizadorId is not Guid utilizadorId) return Unauthorized();
        return ConverterResultado(await participacaoService.CancelarAsync(id, utilizadorId, ct));
    }
}
