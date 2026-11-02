
using System.IdentityModel.Tokens.Jwt;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class AutenticacaoFluxoIntegrationTests
{
    private sealed class EmailFalso : IServicoEmail
    {
        public string? TokenEnviado { get; private set; }
        public string? Destinatario { get; private set; }

        public Task EnviarConfirmacaoAsync(
            string destino, string token, CancellationToken ct)
        {
            Destinatario = destino;
            TokenEnviado = token;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task IT001_IT003_IT004_RegistoConfirmacaoLogin_FluxoValido()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-exclusiva-para-testes-de-integracao-com-mais-de-32-caracteres",
                ["Jwt:Issuer"] = "CampusCarpool.Tests",
                ["Jwt:Audience"] = "CampusCarpool.Tests"
            })
            .Build();

        var email = new EmailFalso();

        var controller = new AutenticacaoController(
            db, configuracao, email);

        const string endereco = "aluno@estg.ipp.pt";
        const string password = "PasswordSegura123!";

        // IT-001: registo válido.
        var registo = await controller.Registar(
            new("Aluno Teste", endereco, password),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(registo);

        var utilizador = await db.Utilizadores.SingleAsync();

        Assert.Equal(endereco, utilizador.Email);
        Assert.False(utilizador.EmailConfirmado);
        Assert.NotEqual(password, utilizador.PasswordHash);

        Assert.Equal(endereco, email.Destinatario);
        Assert.False(string.IsNullOrWhiteSpace(email.TokenEnviado));

        // IT-003: confirmação da conta com o token recebido.
        var confirmacao = await controller.ConfirmarConta(
            new(email.TokenEnviado!),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(confirmacao);

        await db.Entry(utilizador).ReloadAsync();

        Assert.True(utilizador.EmailConfirmado);

        // IT-004: login válido após confirmação.
        var login = await controller.IniciarSessao(
            new(endereco, password),
            CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(login);

        var json = System.Text.Json.JsonSerializer.Serialize(
            resposta.Value);

        using var documento =
            System.Text.Json.JsonDocument.Parse(json);

        var tokenJwt = documento.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(tokenJwt));

        var jwt = new JwtSecurityTokenHandler()
            .ReadJwtToken(tokenJwt);

        Assert.Equal(utilizador.Id.ToString(), jwt.Subject);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }
}
