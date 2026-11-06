using System.Security.Claims;
using System.Text.Json;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — DEF-BE-006: perfil público de outro utilizador com a classificação média
// (RF-F001-17, RF-F001-19, RF-F007-10; P-10). SQLite em memória, controller direto,
// AvaliacaoService real (o mesmo arredondamento de GET /avaliacoes/utilizadores/{id}).
public sealed class PerfilPublicoTests : IDisposable
{
    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;

    public PerfilPublicoTests()
    {
        _ligacao.Open();
        _db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseBaseDeTeste(_ligacao).Options);
        _db.Database.CriarEsquemaDeTeste();
    }

    public void Dispose()
    {
        _db.Dispose();
        _ligacao.Dispose();
    }

    private Utilizador CriarUtilizador(string nome)
    {
        var utilizador = new Utilizador
        {
            Nome = nome,
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            PasswordHash = "HASH_SECRETO",
            EmailConfirmado = true,
            Curso = "Engenharia Informática",
            Instituicao = "ESTG"
        };
        _db.Utilizadores.Add(utilizador);
        return utilizador;
    }

    private UtilizadoresController CriarController(Guid utilizadorId) => new(ServicosDeTeste.CriarPerfil(_db))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, utilizadorId.ToString())], "Test"))
            }
        }
    };

    private Task<IActionResult> ConsultarAsync(Guid quem, Guid perfil) =>
        CriarController(quem).ConsultarPerfilPublico(
            perfil, default);

    [Fact]
    public async Task I110_20_PerfilPublico_DevolveCamposDeP10ComMedia()
    {
        var consultado = CriarUtilizador("Condutora");
        var avaliadorA = CriarUtilizador("A");
        var avaliadorB = CriarUtilizador("B");
        var viagem = new Viagem
        {
            DestinoLatitude = 41.55m,
            CondutorId = consultado.Id,
            Origem = "ESTG",
            Destino = "Porto",
            PartidaEm = DateTime.UtcNow.AddDays(-1),
            LugaresOferecidos = 2,
            LugaresDisponiveis = 0,
            Estado = EstadoViagem.Concluida
        };
        _db.Viagens.Add(viagem);
        _db.Avaliacoes.AddRange(
            new Avaliacao { ViagemId = viagem.Id, AvaliadorId = avaliadorA.Id, AvaliadoId = consultado.Id, Classificacao = 4 },
            new Avaliacao { ViagemId = viagem.Id, AvaliadorId = avaliadorB.Id, AvaliadoId = consultado.Id, Classificacao = 5 });
        _db.LigacoesExternas.AddRange(
            new LigacaoExterna { UtilizadorId = consultado.Id, Url = "https://exemplo.pt/2", Ordem = 2 },
            new LigacaoExterna { UtilizadorId = consultado.Id, Url = "https://exemplo.pt/1", Descricao = "Portefólio", Ordem = 1 });
        await _db.SaveChangesAsync();

        var ok = Assert.IsType<OkObjectResult>(await ConsultarAsync(avaliadorA.Id, consultado.Id));
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;

        Assert.Equal(consultado.Id, raiz.GetProperty("Id").GetGuid());
        Assert.Equal("Condutora", raiz.GetProperty("Nome").GetString());
        Assert.Equal("Engenharia Informática", raiz.GetProperty("Curso").GetString());
        Assert.Equal("ESTG", raiz.GetProperty("Instituicao").GetString());
        Assert.Equal(4.5m, raiz.GetProperty("ClassificacaoMedia").GetDecimal());
        Assert.Equal(2, raiz.GetProperty("TotalAvaliacoes").GetInt32());
        Assert.Equal(
            ["https://exemplo.pt/1", "https://exemplo.pt/2"],
            raiz.GetProperty("LigacoesExternas").EnumerateArray().Select(l => l.GetProperty("Url").GetString()));

        // RF-F001-17: nenhum campo fora de P-10.
        Assert.Equal(
            ["Id", "Nome", "Curso", "Instituicao", "LigacoesExternas", "ClassificacaoMedia", "TotalAvaliacoes"],
            raiz.EnumerateObject().Select(p => p.Name));
        Assert.DoesNotContain(consultado.Email, json);
        Assert.DoesNotContain("HASH_SECRETO", json);
    }

    [Fact]
    public async Task I110_21_PerfilPublicoSemAvaliacoes_MediaNula()
    {
        var consultado = CriarUtilizador("Sem avaliações");
        var outro = CriarUtilizador("Outro");
        await _db.SaveChangesAsync();

        var ok = Assert.IsType<OkObjectResult>(await ConsultarAsync(outro.Id, consultado.Id));
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("ClassificacaoMedia").ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("TotalAvaliacoes").GetInt32());
    }

    [Fact]
    public async Task I110_22_PerfilPublicoInexistente_Devolve404()
    {
        var quem = CriarUtilizador("Quem");
        await _db.SaveChangesAsync();

        Assert.IsType<NotFoundObjectResult>(await ConsultarAsync(quem.Id, Guid.NewGuid()));
    }
}
