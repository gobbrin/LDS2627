using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — DEF-BE-004 (RF-F001-12): depois do fim de sessão, o token deixa de ser aceite.
// Ao contrário dos outros testes HTTP, mantém o JwtBearer real do Program.cs (TE-04):
// o token é emitido pelo próprio login com a chave de teste. Só a base de dados é
// trocada por SQLite em memória.
public sealed class TerminarSessaoHttpTests : IDisposable
{
    private const string Email = "sessao@estg.ipp.pt";
    private const string Password = "Palavra-passe-de-teste-1";

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _ligacao = new("Data Source=:memory:");

        public ApiFactory() => _ligacao.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(local);Database=CampusCarpool_Tests;");
            builder.UseSetting("Jwt:Key", "CHAVE_DE_TESTES_I110_COM_MAIS_DE_32_BYTES_12345");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CampusCarpoolDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CampusCarpoolDbContext>>();
                services.AddDbContext<CampusCarpoolDbContext>(options => options.UseSqlite(_ligacao));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _ligacao.Dispose();
        }
    }

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _cliente;

    public TerminarSessaoHttpTests()
    {
        _cliente = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>();
        db.Database.EnsureCreated();
        var utilizador = new Utilizador { Nome = "Sessão", Email = Email, EmailConfirmado = true };
        utilizador.PasswordHash = new PasswordHasher<Utilizador>().HashPassword(utilizador, Password);
        db.Utilizadores.Add(utilizador);
        db.SaveChanges();
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _factory.Dispose();
    }

    private async Task<string> IniciarSessaoAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/v1/autenticacao/login", new { email = Email, password = Password });
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpStatusCode> EnviarAsync(HttpMethod metodo, string rota, string token)
    {
        using var pedido = new HttpRequestMessage(metodo, rota);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resposta = await _cliente.SendAsync(pedido);
        return resposta.StatusCode;
    }

    [Fact]
    public async Task I110_17_TerminarSessao_TokenDeixaDeSerAceite()
    {
        var token = await IniciarSessaoAsync();
        Assert.Equal(HttpStatusCode.OK, await EnviarAsync(HttpMethod.Get, "/api/v1/autenticacao/me", token));

        Assert.Equal(HttpStatusCode.NoContent, await EnviarAsync(HttpMethod.Post, "/api/v1/autenticacao/logout", token));

        Assert.Equal(HttpStatusCode.Unauthorized, await EnviarAsync(HttpMethod.Get, "/api/v1/autenticacao/me", token));
        Assert.Equal(HttpStatusCode.Unauthorized, await EnviarAsync(HttpMethod.Post, "/api/v1/autenticacao/logout", token));
    }

    [Fact]
    public async Task I110_18_TerminarSessao_NaoAfetaOutraSessaoDoMesmoUtilizador()
    {
        var tokenA = await IniciarSessaoAsync();
        var tokenB = await IniciarSessaoAsync();

        Assert.Equal(HttpStatusCode.NoContent, await EnviarAsync(HttpMethod.Post, "/api/v1/autenticacao/logout", tokenA));

        Assert.Equal(HttpStatusCode.OK, await EnviarAsync(HttpMethod.Get, "/api/v1/autenticacao/me", tokenB));
    }

    [Fact]
    public async Task I110_19_TerminarSessaoSemToken_Devolve401()
    {
        using var resposta = await _cliente.PostAsync("/api/v1/autenticacao/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
