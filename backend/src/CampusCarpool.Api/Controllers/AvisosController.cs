using CampusCarpool.Application.Comunicacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Emissão e consulta de avisos de uma viagem (F005). Rotas em <c>/api/v1/viagens/{viagemId}/avisos</c>.</summary>
/// <remarks>As regras de acesso e a criação das notificações estão no <see cref="IAvisoService"/>.</remarks>
[ApiController, Authorize, Route("api/v1/viagens/{viagemId:guid}/avisos")]
public sealed class AvisosController(IAvisoService avisoService) : ControllerBase
{
    /// <summary>Corpo de <c>POST</c>: o texto do aviso (até 100 caracteres, P-28).</summary>
    public sealed record CriarAviso(string? Conteudo);

    /// <summary>Cria um aviso e notifica os passageiros com participação aceite.</summary>
    /// <returns>201; 400 texto inválido; 401; 403 não é o condutor; 404 viagem inexistente; 409 viagem não ativa.</returns>
    [HttpPost]
    public async Task<IActionResult> Criar(Guid viagemId, CriarAviso? input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid condutorId) return Unauthorized();

        return this.Responder(
            await avisoService.CriarAsync(viagemId, condutorId, input?.Conteudo, ct),
            // Aponta para a lista: não existe rota para um aviso individual.
            aviso => Created($"/api/v1/viagens/{viagemId}/avisos", aviso));
    }

    /// <summary>Lista os avisos quando o utilizador é condutor ou passageiro aceite.</summary>
    /// <returns>200 avisos (mais recentes primeiro); 401; 403; 404.</returns>
    [HttpGet]
    public async Task<IActionResult> Listar(Guid viagemId, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid utilizadorId) return Unauthorized();

        return this.Responder(await avisoService.ListarAsync(viagemId, utilizadorId, ct), avisos => Ok(avisos));
    }
}
