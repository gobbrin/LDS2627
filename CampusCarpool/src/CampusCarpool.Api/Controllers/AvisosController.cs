using System.Security.Claims;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Endpoints para emissão e consulta de avisos de uma viagem.</summary>
/// <remarks>
/// O controller trata HTTP e identidade. As regras de acesso, persistência e
/// criação das notificações vivem em <see cref="IAvisoService"/>.
/// </remarks>
[ApiController, Authorize, Route("api/v1/viagens/{viagemId:guid}/avisos")]
public sealed class AvisosController(IAvisoService avisoService) : ControllerBase
{
    public sealed record CriarAviso(string? Conteudo);

    private Guid? UtilizadorId => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        out var id) ? id : null;

    /// <summary>Cria um aviso e notifica os passageiros com participação aceite.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar(Guid viagemId, CriarAviso? input, CancellationToken ct)
    {   
        if (UtilizadorId is not Guid condutorId) return Unauthorized();

        var resultado = await avisoService.CriarAsync(viagemId, condutorId, input?.Conteudo, ct);
        return resultado.Resultado switch
        {
            ResultadoCriacaoAviso.Sucesso => Created(
                // CORREÇÃO: Aponta para a rota da lista de avisos (que existe), 
                // em vez de apontar para um ID individual (que não existe na I044).
                $"/api/v1/viagens/{viagemId}/avisos", 
                new { resultado.Aviso!.Id, resultado.Aviso.Conteudo, resultado.Aviso.CriadoEm }),
            ResultadoCriacaoAviso.ViagemNaoEncontrada => NotFound(),
            ResultadoCriacaoAviso.NaoAutorizado => Forbid(),
            _ => BadRequest(new { erro = "Conteúdo do aviso inválido." })
        };
    }

    /// <summary>Lista os avisos quando o utilizador é condutor ou passageiro aceite.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(Guid viagemId, CancellationToken ct)
    {
        if (UtilizadorId is not Guid utilizadorId) return Unauthorized();

        var resultado = await avisoService.ListarAsync(viagemId, utilizadorId, ct);
        return resultado.Resultado switch
        {
            ResultadoConsultaAvisos.Sucesso => Ok(resultado.Avisos),
            ResultadoConsultaAvisos.ViagemNaoEncontrada => NotFound(),
            _ => Forbid()
        };
    }
}
