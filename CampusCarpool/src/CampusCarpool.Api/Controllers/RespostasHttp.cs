using CampusCarpool.Application.Comum;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>Traduz os resultados da Application em respostas HTTP.</summary>
/// <remarks>É o único sítio que associa cada <see cref="TipoErro"/> a um código. O corpo de erro é <c>{ erro }</c>.</remarks>
internal static class RespostasHttp
{
    /// <summary>Resposta de um resultado com valor.</summary>
    public static IActionResult Responder<T>(this ControllerBase controller, Resultado<T> resultado, Func<T, IActionResult> sucesso) =>
        resultado.Erro is { } erro ? controller.RespostaDeErro(erro) : sucesso(resultado.Valor);

    /// <summary>Resposta de um resultado sem valor.</summary>
    public static IActionResult Responder(this ControllerBase controller, Resultado resultado, Func<IActionResult> sucesso) =>
        resultado.Erro is { } erro ? controller.RespostaDeErro(erro) : sucesso();

    /// <summary>Resposta de um erro: com corpo <c>{ erro }</c> se tiver mensagem, sem corpo se não tiver.</summary>
    public static IActionResult RespostaDeErro(this ControllerBase controller, Erro erro)
    {
        var corpo = erro.Mensagem is null ? null : new CorpoErro(erro.Mensagem);

        return erro.Tipo switch
        {
            TipoErro.DadosInvalidos => corpo is null ? controller.BadRequest() : controller.BadRequest(corpo),
            TipoErro.NaoAutenticado => corpo is null ? controller.Unauthorized() : controller.Unauthorized(corpo),
            TipoErro.Proibido => corpo is null ? controller.Forbid() : controller.StatusCode(StatusCodes.Status403Forbidden, corpo),
            TipoErro.NaoEncontrado => corpo is null ? controller.NotFound() : controller.NotFound(corpo),
            TipoErro.Conflito => corpo is null ? controller.Conflict() : controller.Conflict(corpo),
            TipoErro.NaoProcessavel => corpo is null ? controller.UnprocessableEntity() : controller.UnprocessableEntity(corpo),
            TipoErro.FalhaServicoExterno => controller.StatusCode(StatusCodes.Status502BadGateway, corpo),
            TipoErro.ServicoIndisponivel => controller.StatusCode(StatusCodes.Status503ServiceUnavailable, corpo),
            TipoErro.TempoEsgotado => controller.StatusCode(StatusCodes.Status504GatewayTimeout, corpo),
            _ => throw new ArgumentOutOfRangeException(nameof(erro), erro.Tipo, "Tipo de erro desconhecido.")
        };
    }
}
