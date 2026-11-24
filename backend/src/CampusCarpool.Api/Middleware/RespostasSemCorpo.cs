using CampusCarpool.Api.Controllers;
using Microsoft.AspNetCore.Diagnostics;

namespace CampusCarpool.Api.Middleware;

/// <summary>
/// Dá corpo <c>{ erro }</c> às respostas de erro que o ASP.NET Core devolve sem corpo (BE-08, I112).
/// </summary>
/// <remarks>
/// São as que não passam por um controller nem pelo <c>GlobalExceptionHandler</c>: rota desconhecida com sessão (404),
/// método errado (405), corpo que não é JSON (415) e recusa da autorização (403). Assim o frontend lê todos os erros
/// da mesma maneira. O 401 continua sem corpo: é a única exceção do contrato (o JwtBearer responde assim).
/// Registado com <c>app.UseStatusCodePages(RespostasSemCorpo.EscreverErroAsync)</c>, que só chama este método quando
/// a resposta tem um código de erro e ainda não tem corpo.
/// </remarks>
public static class RespostasSemCorpo
{
    /// <summary>Escreve <c>{ erro }</c> com uma mensagem para o código da resposta, exceto no 401.</summary>
    /// <param name="contexto">Contexto da resposta, já com o código de erro definido.</param>
    public static async Task EscreverErroAsync(StatusCodeContext contexto)
    {
        HttpResponse resposta = contexto.HttpContext.Response;

        // 401 sem corpo, como o do JwtBearer: o frontend trata-o sempre como «sessão terminada».
        if (resposta.StatusCode == StatusCodes.Status401Unauthorized)
            return;

        CorpoErro corpo = new(MensagemPara(resposta.StatusCode));
        await resposta.WriteAsJsonAsync(corpo, contexto.HttpContext.RequestAborted);
    }

    /// <summary>Mensagem para o utilizador de cada código que pode chegar aqui sem corpo.</summary>
    /// <param name="codigo">Código HTTP da resposta.</param>
    /// <returns>Texto em português; um texto genérico para os códigos não previstos.</returns>
    public static string MensagemPara(int codigo)
    {
        switch (codigo)
        {
            case StatusCodes.Status400BadRequest:
                return "Pedido inválido.";
            case StatusCodes.Status403Forbidden:
                return "Sem permissão para esta operação.";
            case StatusCodes.Status404NotFound:
                return "Endereço não encontrado.";
            case StatusCodes.Status405MethodNotAllowed:
                return "Método não permitido neste endereço.";
            case StatusCodes.Status415UnsupportedMediaType:
                return "O corpo do pedido tem de ser JSON (Content-Type: application/json).";
            default:
                if (codigo >= StatusCodes.Status500InternalServerError)
                    return "Ocorreu um erro inesperado.";
                return "Pedido inválido.";
        }
    }
}
