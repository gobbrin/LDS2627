using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class EndpointsAdditionalTests
{

    private static EstimativaCustoService Custos()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custos:PrecoPorKm"] = "0.10"
            })
            .Build();

        return new EstimativaCustoService(config);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }
        public Fixture()
        {
            connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(connection).Options);
            Db.Database.EnsureCreated();
        }
        public void Dispose() { Db.Dispose(); connection.Dispose(); }
        public async Task<Utilizador> User(string email = "user@estg.ipp.pt", bool confirmed = true)
        {
            var u = new Utilizador { Nome = "Teste", Email = email, EmailConfirmado = confirmed };
            Db.Utilizadores.Add(u); await Db.SaveChangesAsync(); return u;
        }
        public async Task<Viagem> Trip(Guid owner, string estado = "Ativa", int available = 2,
            DateTime? partida = null, string origem = "Porto", string destino = "Braga")
        {
            var v = new Viagem
            {
                CondutorId = owner,
                Origem = origem,
                Destino = destino,
                Estado = estado,
                PartidaEm = partida ?? DateTime.UtcNow.AddDays(5),
                LugaresOferecidos = 3,
                LugaresDisponiveis = available
            };
            Db.Viagens.Add(v); await Db.SaveChangesAsync(); return v;
        }
    }
    private static void Auth(ControllerBase c, Guid id) => c.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "Test"))
        }
    };
    private static MapasService Maps() => new(new HttpClient(), new ConfigurationBuilder().Build());
    private static ViagensController Trips(Fixture f) => new(f.Db, Maps());
    private sealed class EmailFake : IServicoEmail
    {
        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct) => Task.CompletedTask;
    }

    private static AutenticacaoController AuthController(Fixture f) =>
        new(f.Db, new ConfigurationBuilder().Build(), new EmailFake());
    private static int Total(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
    }

    [Fact]
    public async Task LoginSemCredenciais_Devolve401()
    {
        using var f = new Fixture();
        Assert.IsType<UnauthorizedResult>(await AuthController(f).IniciarSessao(new("", ""), default));
    }
    [Fact]
    public async Task LoginContaNaoConfirmada_Devolve403()
    {
        using var f = new Fixture(); var u = await f.User(confirmed: false);
        u.PasswordHash = new PasswordHasher<Utilizador>().HashPassword(u, "PasswordSegura123!");
        await f.Db.SaveChangesAsync();
        var r = await AuthController(f).IniciarSessao(new(u.Email, "PasswordSegura123!"), default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(r).StatusCode);
    }
    [Fact]
    public async Task ConfirmarTokenVazio_Devolve400()
    {
        using var f = new Fixture();
        Assert.IsType<BadRequestResult>(await AuthController(f).ConfirmarConta(new(" "), default));
    }
    [Fact]
    public async Task ReenviarConfirmacaoEmailVazio_Devolve400()
    {
        using var f = new Fixture();
        Assert.IsType<BadRequestObjectResult>(await AuthController(f).ReenviarConfirmacaoEmail(new(" "), default));
    }
    [Fact]
    public async Task ReenviarConfirmacaoEmailDesconhecido_RespostaNeutra()
    {
        using var f = new Fixture();
        Assert.IsType<AcceptedResult>(
    await AuthController(f).ReenviarConfirmacaoEmail(
        new("desconhecido@estg.ipp.pt"), default));
    }
    [Fact]
    public void MeAutenticacao_DevolveIdDoToken()
    {
        using var f = new Fixture(); var c = AuthController(f); var id = Guid.NewGuid(); Auth(c, id);
        var r = Assert.IsType<OkObjectResult>(c.Me());
        Assert.Contains(id.ToString(), JsonSerializer.Serialize(r.Value));
    }
    [Fact]
    public async Task PerfilSemIdentidade_Devolve401()
    {
        using var f = new Fixture(); var c = new UtilizadoresController(f.Db);
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Assert.IsType<UnauthorizedResult>(await c.ConsultarPerfil(default));
    }
    [Fact]
    public async Task PerfilUtilizadorInexistente_Devolve404()
    {
        using var f = new Fixture(); var c = new UtilizadoresController(f.Db); Auth(c, Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(await c.ConsultarPerfil(default));
    }
    [Fact]
    public async Task AtualizarPerfilInexistente_Devolve404()
    {
        using var f = new Fixture(); var c = new UtilizadoresController(f.Db); Auth(c, Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(await c.Atualizar(new("Novo", null, null), default));
    }
    [Fact]
    public async Task AtualizarPerfilNomeMuitoLongo_Devolve400()
    {
        using var f = new Fixture(); var c = new UtilizadoresController(f.Db); Auth(c, Guid.NewGuid());
        Assert.IsType<BadRequestObjectResult>(await c.Atualizar(new(new string('A', 101), null, null), default));
    }
    [Fact]
    public async Task CriarViagemSemIdentidade_Devolve401()
    {
        using var f = new Fixture(); var c = Trips(f);
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Assert.IsType<UnauthorizedResult>(await c.Criar(new("Porto", "Braga", DateTime.UtcNow.AddDays(2), 2), default));
    }
    [Fact]
    public async Task CriarViagemContaNaoConfirmada_Devolve403()
    {
        using var f = new Fixture(); var u = await f.User(confirmed: false); var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<ForbidResult>(await c.Criar(new("Porto", "Braga", DateTime.UtcNow.AddDays(2), 2), default));
    }
    [Theory]
    [InlineData("Porto", "Porto", 2)]
    [InlineData("", "Braga", 2)]
    [InlineData("Porto", "Braga", 0)]
    [InlineData("Porto", "Braga", 9)]
    public async Task CriarViagemDadosInvalidos_Devolve400(string origem, string destino, int lugares)
    {
        using var f = new Fixture(); var u = await f.User(); var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<BadRequestObjectResult>(await c.Criar(new(origem, destino, DateTime.UtcNow.AddDays(2), lugares), default));
    }
    [Fact]
    public async Task CriarViagemSemMaps_Devolve503()
    {
        using var f = new Fixture(); var u = await f.User(); var c = Trips(f); Auth(c, u.Id);
        Assert.Equal(503, Assert.IsType<ObjectResult>(await c.Criar(
            new("Porto", "Braga", DateTime.UtcNow.AddDays(2), 2), default)).StatusCode);
    }
    [Fact]
    public async Task ConsultarViagemExistente_Devolve200()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id);
        Assert.IsType<OkObjectResult>(await Trips(f).Consultar(v.Id, default));
    }
    [Fact]
    public async Task EditarViagemInexistente_Devolve404()
    {
        using var f = new Fixture(); var u = await f.User(); var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<NotFoundResult>(await c.Editar(Guid.NewGuid(),
            new(DateTime.UtcNow.AddDays(3), 3), default));
    }
    [Fact]
    public async Task EditarViagemAtiva_AtualizaLugares()
    {
        using var f = new Fixture();

        var u = await f.User();
        var v = await f.Trip(u.Id);

        var ocupadosAntes =
            v.LugaresOferecidos - v.LugaresDisponiveis;

        var c = Trips(f);
        Auth(c, u.Id);

        Assert.IsType<NoContentResult>(
            await c.Editar(
                v.Id,
                new(DateTime.UtcNow.AddDays(6), 4),
                default
            )
        );

        var viagemAtualizada =
            await f.Db.Viagens.FindAsync(v.Id);

        Assert.NotNull(viagemAtualizada);

        Assert.Equal(4, viagemAtualizada.LugaresOferecidos);

        Assert.Equal(
            4 - ocupadosAntes,
            viagemAtualizada.LugaresDisponiveis
        );
    }
    [Fact]
    public async Task EditarViagemInativa_Devolve409()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id, "Indisponibilizada");
        var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<ConflictObjectResult>(await c.Editar(v.Id,
            new(DateTime.UtcNow.AddDays(6), 3), default));
    }
    [Fact]
    public async Task IndisponibilizarViagem_AlteraEstado()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id);
        var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<NoContentResult>(await c.Indisponibilizar(v.Id, default));
        Assert.Equal("Indisponibilizada", (await f.Db.Viagens.FindAsync(v.Id))!.Estado);
    }
    [Fact]
    public async Task IndisponibilizarViagemDeOutroCondutor_Devolve403()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id);
        var c = Trips(f); Auth(c, Guid.NewGuid());
        Assert.IsType<ForbidResult>(await c.Indisponibilizar(v.Id, default));
    }
    [Fact]
    public async Task ConcluirViagemFutura_Devolve409()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id);
        var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<ConflictResult>(await c.Concluir(v.Id, default));
    }
    [Fact]
    public async Task ConcluirViagemPassada_AlteraEstado()
    {
        using var f = new Fixture(); var u = await f.User(); var v = await f.Trip(u.Id,
            partida: DateTime.UtcNow.AddDays(-1)); var c = Trips(f); Auth(c, u.Id);
        Assert.IsType<NoContentResult>(await c.Concluir(v.Id, default));
        Assert.Equal("Concluída", (await f.Db.Viagens.FindAsync(v.Id))!.Estado);
    }
    [Fact]
    public async Task PesquisaExcluiViagensSemLugaresEPassadas()
    {
        using var f = new Fixture(); var u = await f.User();
        await f.Trip(u.Id, available: 0); await f.Trip(u.Id, partida: DateTime.UtcNow.AddDays(-1));
        await f.Trip(u.Id);
        Assert.Equal(1, Total(await new PesquisaController(f.Db).Pesquisar(null, null, null, null, 1, 20, default)));
    }
    [Fact]
    public async Task PesquisaFiltraOrigemDestino()
    {
        using var f = new Fixture(); var u = await f.User();
        await f.Trip(u.Id); await f.Trip(u.Id, origem: "Lisboa", destino: "Coimbra");
        Assert.Equal(1, Total(await new PesquisaController(f.Db).Pesquisar(
            "Porto", "Braga", null, null, 1, 20, default)));
    }
    [Fact]
    public async Task PesquisaPaginacaoRespeitaTamanho()
    {
        using var f = new Fixture(); var u = await f.User();
        await f.Trip(u.Id); await f.Trip(u.Id);
        var result = Assert.IsType<OkObjectResult>(await new PesquisaController(f.Db).Pesquisar(
            null, null, null, null, 2, 1, default));
        Assert.Equal(2, Total(result));
        var items = (System.Collections.IEnumerable)result.Value!.GetType().GetProperty("viagens")!.GetValue(result.Value)!;
        Assert.Single(items.Cast<object>());
    }
    [Fact]
    public async Task GeocodificarSemChave_Devolve503()
    {
        var r = await new RotasController(Maps(), Custos()).Geocodificar("Porto", default);
        Assert.Equal(503, Assert.IsType<ObjectResult>(r).StatusCode);
    }
    [Fact]
    public async Task CalcularOrigemVazia_Devolve400()
    {
        Assert.IsType<BadRequestResult>(await new RotasController(Maps(), Custos()).Calcular(new(" ", "Braga"), default));
    }
}
