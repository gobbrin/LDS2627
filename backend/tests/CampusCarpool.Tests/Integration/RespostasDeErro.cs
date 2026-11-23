using System.Net;
using System.Text.Json;
using CampusCarpool.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusCarpool.Tests;

/// <summary>
/// Verificações das respostas de erro: o código e a mensagem do corpo <c>{ erro }</c> (BE-09, I112).
/// </summary>
/// <remarks>
/// Verificar só o código deixava passar um erro pelo motivo errado (por exemplo, um email não académico recusado
/// pela palavra-passe). Cada teste de erro diz a mensagem que espera.
/// </remarks>
public static class RespostasDeErro
{
    /// <summary>Resposta de um controller: código <paramref name="codigo"/> e corpo <c>{ erro: mensagem }</c>.</summary>
    /// <param name="resposta">Resultado devolvido pela ação do controller.</param>
    /// <param name="codigo">Código HTTP esperado (400, 404, 409…).</param>
    /// <param name="mensagem">Texto exato do campo <c>erro</c>.</param>
    public static void AssertErro(IActionResult resposta, int codigo, string mensagem)
    {
        ObjectResult resultado = Assert.IsAssignableFrom<ObjectResult>(resposta);
        Assert.Equal(codigo, resultado.StatusCode);

        CorpoErro corpo = Assert.IsType<CorpoErro>(resultado.Value);
        Assert.Equal(mensagem, corpo.Erro);
    }

    /// <summary>Resposta HTTP: código <paramref name="codigo"/> e campo <c>erro</c> igual a <paramref name="mensagem"/>.</summary>
    /// <param name="resposta">Resposta do pipeline HTTP.</param>
    /// <param name="codigo">Código HTTP esperado.</param>
    /// <param name="mensagem">Texto exato do campo <c>erro</c>.</param>
    public static async Task AssertErroAsync(HttpResponseMessage resposta, HttpStatusCode codigo, string mensagem)
    {
        string texto = await resposta.Content.ReadAsStringAsync();

        // A mensagem de falha mostra o corpo, para se perceber logo porque o código não é o esperado.
        Assert.True(codigo == resposta.StatusCode, $"Esperado {(int)codigo}, recebido {(int)resposta.StatusCode}: {texto}");

        using JsonDocument json = JsonDocument.Parse(texto);
        Assert.Equal(mensagem, json.RootElement.GetProperty("erro").GetString());
    }
}
