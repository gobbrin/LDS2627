using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Ligações a perfis externos (DEF-BE-005; RF-F001-15, RF-F001-16, RF-F001-20, RF-F001-22, P-27).</summary>
/// <remarks>SQLite em memória; o controller é criado diretamente.</remarks>
public sealed class LigacoesExternasTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador dono = new() { Nome = "Dono", Email = "dono@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador outro = new() { Nome = "Outro", Email = "outro@estg.ipp.pt", EmailConfirmado = true };

    public LigacoesExternasTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseSqlite(ligacao).Options);
        db.Database.EnsureCreated();
        db.AddRange(dono, outro);
        db.SaveChanges();
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    private UtilizadoresController Controller(Guid utilizadorId) => new(db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, utilizadorId.ToString())], "Teste"))
            }
        }
    };

    [Fact]
    public async Task I110_54_AdicionarLigacao_ApareceNoPerfil()
    {
        var resultado = await Controller(dono.Id).AdicionarLigacao(
            new("https://github.com/exemplo", " GitHub "), default);

        Assert.Equal(201, Assert.IsType<ObjectResult>(resultado).StatusCode);

        var perfil = Assert.IsType<OkObjectResult>(await Controller(dono.Id).ConsultarPerfil(default)).Value!;
        var ligacoes = Assert.IsType<List<UtilizadoresController.LigacaoPerfil>>(
            perfil.GetType().GetProperty("LigacoesExternas")!.GetValue(perfil));
        var ligacaoGuardada = Assert.Single(ligacoes);
        Assert.Equal("https://github.com/exemplo", ligacaoGuardada.Url);
        Assert.Equal("GitHub", ligacaoGuardada.Descricao);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("github.com/exemplo")]
    [InlineData("ftp://exemplo.pt")]
    [InlineData("")]
    public async Task I110_55_LigacaoQueNaoEHttp_Devolve400(string url)
    {
        Assert.IsType<BadRequestObjectResult>(await Controller(dono.Id).AdicionarLigacao(new(url, null), default));
        Assert.Empty(db.LigacoesExternas);
    }

    [Fact]
    public async Task I110_56_MaximoDeCincoLigacoes()
    {
        for (var i = 0; i < 5; i++)
            Assert.IsType<ObjectResult>(await Controller(dono.Id).AdicionarLigacao(new($"https://exemplo.pt/{i}", null), default));

        Assert.IsType<BadRequestObjectResult>(await Controller(dono.Id).AdicionarLigacao(new("https://exemplo.pt/6", null), default));
        Assert.Equal(5, await db.LigacoesExternas.CountAsync());
    }

    [Fact]
    public async Task I110_57_RemoverLigacao_SoODono()
    {
        var criada = Assert.IsType<UtilizadoresController.LigacaoPerfil>(
            Assert.IsType<ObjectResult>(await Controller(dono.Id).AdicionarLigacao(new("https://exemplo.pt", null), default)).Value);

        Assert.IsType<NotFoundObjectResult>(await Controller(outro.Id).RemoverLigacao(criada.Id, default));
        Assert.Equal(1, await db.LigacoesExternas.CountAsync());

        Assert.IsType<NoContentResult>(await Controller(dono.Id).RemoverLigacao(criada.Id, default));
        Assert.Empty(db.LigacoesExternas);
    }
}
