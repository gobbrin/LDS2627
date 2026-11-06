using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Confirmação de conta pela API real: motivo de cada recusa (RF-F001-08) e reenvio da ligação (RF-F001-21).</summary>
/// <remarks>Sobre a <see cref="AplicacaoReal"/>; o token vem do email falso, como viria da ligação recebida.</remarks>
public sealed class ConfirmacaoContaHttpTests : IDisposable
{
    private const string Email = "confirmacao@estg.ipp.pt";

    private readonly AplicacaoReal _app = new();

    public void Dispose() => _app.Dispose();

    private async Task<string> RegistarAsync(HttpClient cliente)
    {
        using var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/registo",
            new { nome = "Conta", email = Email, password = AplicacaoReal.PalavraPasse });

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        return _app.Email.Tokens.Last();
    }

    private static Task<HttpResponseMessage> ConfirmarAsync(HttpClient cliente, string token) =>
        cliente.PostAsJsonAsync("/api/v1/autenticacao/confirmar", new { token });

    private static async Task<string> ErroDe(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return corpo!["erro"];
    }

    // Recua a data de todas as ligações: a validade é contada a partir da expiração.
    private void RecuarLigacoes(TimeSpan quanto) => _app.NaBaseDeDados(db =>
    {
        foreach (var confirmacao in db.ConfirmacoesConta.ToList())
            confirmacao.ExpiraEm -= quanto;

        return db.SaveChanges();
    });

    [Fact]
    public async Task I110_114_LigacaoExpirada_DizQueExpirou()
    {
        using var cliente = _app.CreateClient();
        var token = await RegistarAsync(cliente);
        RecuarLigacoes(TimeSpan.FromHours(25));

        using var resposta = await ConfirmarAsync(cliente, token);

        Assert.Contains("expirou", await ErroDe(resposta), StringComparison.Ordinal);
    }

    [Fact]
    public async Task I110_115_LigacaoJaUsada_DizQueFoiUsada()
    {
        using var cliente = _app.CreateClient();
        var token = await RegistarAsync(cliente);

        using (var primeira = await ConfirmarAsync(cliente, token))
            Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);

        using var segunda = await ConfirmarAsync(cliente, token);

        Assert.Contains("já foi usada", await ErroDe(segunda), StringComparison.Ordinal);
    }

    [Fact]
    public async Task I110_116_LigacaoDesconhecida_DizQueEInvalida()
    {
        using var cliente = _app.CreateClient();

        using var resposta = await ConfirmarAsync(cliente, "token-que-nunca-existiu");

        Assert.Contains("inválida", await ErroDe(resposta), StringComparison.Ordinal);
    }

    [Fact]
    public async Task I110_117_Reenvio_InvalidaAsLigacoesAnteriores()
    {
        using var cliente = _app.CreateClient();
        var antigo = await RegistarAsync(cliente);
        RecuarLigacoes(TimeSpan.FromMinutes(2)); // Passa o limite de um reenvio por minuto; a ligação continua no prazo.

        using (var reenvio = await cliente.PostAsJsonAsync("/api/v1/autenticacao/reenviar-confirmacao", new { email = Email }))
            Assert.Equal(HttpStatusCode.Accepted, reenvio.StatusCode);

        var novo = _app.Email.Tokens.Last();
        Assert.NotEqual(antigo, novo);

        using (var comAntigo = await ConfirmarAsync(cliente, antigo))
            Assert.Contains("expirou", await ErroDe(comAntigo), StringComparison.Ordinal);

        using var comNovo = await ConfirmarAsync(cliente, novo);

        Assert.Equal(HttpStatusCode.NoContent, comNovo.StatusCode);
        Assert.True(_app.NaBaseDeDados(db => db.Utilizadores.Any(u => u.Email == Email && u.EmailConfirmado)));
    }
}
