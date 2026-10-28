using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class ApiIntegrationTests
{
    private sealed class DatabaseFixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }
        public DatabaseFixture()
        {
            connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(connection).Options);
            Db.Database.EnsureCreated();
        }
        public void Dispose() { Db.Dispose(); connection.Dispose(); }
    }

    private static void Authenticate(ControllerBase controller, Guid id)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "Test"))
            }
        };
    }

    private sealed class EmailFake : IServicoEmail
    {
        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task CT008_RegistoEmailNaoAcademico_Devolve400()
    {
        using var fixture = new DatabaseFixture();
        var controller = new AutenticacaoController(fixture.Db, new ConfigurationBuilder().Build(), new EmailFake());
        var result = await controller.Registar(new("Teste", "teste@gmail.com", "PasswordSegura123!"), default);
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(fixture.Db.Utilizadores);
    }

    [Fact]
    public async Task CT009_RegistoPasswordCurta_Devolve400()
    {
        using var fixture = new DatabaseFixture();
        var controller = new AutenticacaoController(fixture.Db, new ConfigurationBuilder().Build(), new EmailFake());
        var result = await controller.Registar(new("Teste", "teste@estg.ipp.pt", "curta"), default);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CT010_RegistoDuplicado_Devolve409()
    {
        using var fixture = new DatabaseFixture();
        fixture.Db.Utilizadores.Add(new Utilizador { Email = "teste@estg.ipp.pt", Nome = "Teste" });
        await fixture.Db.SaveChangesAsync();
        var controller = new AutenticacaoController(fixture.Db, new ConfigurationBuilder().Build(), new EmailFake());
        var result = await controller.Registar(new("Teste", "TESTE@ESTG.IPP.PT", "PasswordSegura123!"), default);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CT013_TokenConfirmacaoInvalido_Devolve400()
    {
        using var fixture = new DatabaseFixture();
        var controller = new AutenticacaoController(fixture.Db, new ConfigurationBuilder().Build(), new EmailFake());
        var result = await controller.ConfirmarConta(new("token-invalido"), default);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CT016_LoginPasswordErrada_Devolve401()
    {
        using var fixture = new DatabaseFixture();
        var controller = new AutenticacaoController(fixture.Db, new ConfigurationBuilder().Build(), new EmailFake());
        var result = await controller.IniciarSessao(new("naoexiste@estg.ipp.pt", "errada"), default);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CT019_PerfilProprio_NaoExpoePasswordHash()
    {
        using var fixture = new DatabaseFixture();
        var user = new Utilizador { Email = "a@estg.ipp.pt", Nome = "A", PasswordHash = "HASH_SECRETO" };
        fixture.Db.Utilizadores.Add(user);
        await fixture.Db.SaveChangesAsync();
        var controller = new UtilizadoresController(fixture.Db);
        Authenticate(controller, user.Id);
        var result = Assert.IsType<OkObjectResult>(await controller.ConsultarPerfil(default));
        Assert.DoesNotContain("PasswordHash", System.Text.Json.JsonSerializer.Serialize(result.Value));
        Assert.DoesNotContain("HASH_SECRETO", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task CT020_AtualizarPerfil_AlteraSoCamposPermitidos()
    {
        using var fixture = new DatabaseFixture();
        var user = new Utilizador { Email = "a@estg.ipp.pt", Nome = "Antes", PasswordHash = "HASH" };
        fixture.Db.Utilizadores.Add(user);
        await fixture.Db.SaveChangesAsync();
        var controller = new UtilizadoresController(fixture.Db);
        Authenticate(controller, user.Id);
        Assert.IsType<OkObjectResult>(await controller.Atualizar(new("Depois", "EI", "ESTG"), default));
        var persisted = await fixture.Db.Utilizadores.FindAsync(user.Id);
        Assert.Equal("Depois", persisted!.Nome);
        Assert.Equal("a@estg.ipp.pt", persisted.Email);
        Assert.Equal("HASH", persisted.PasswordHash);
    }

    [Fact]
    public async Task CT022_NomeVazio_Devolve400()
    {
        using var fixture = new DatabaseFixture();
        var controller = new UtilizadoresController(fixture.Db);
        Authenticate(controller, Guid.NewGuid());
        Assert.IsType<BadRequestObjectResult>(await controller.Atualizar(new(" ", null, null), default));
    }

    [Fact]
    public async Task CT026_ConsultarViagemInexistente_Devolve404()
    {
        using var fixture = new DatabaseFixture();
        var controller = new ViagensController(fixture.Db, new MapasService(
            new HttpClient(), new ConfigurationBuilder().Build()));
        Assert.IsType<NotFoundResult>(await controller.Consultar(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task CT027_OutroCondutor_NaoPodeEditar()
    {
        using var fixture = new DatabaseFixture();
        var owner = new Utilizador { Email = "a@estg.ipp.pt", Nome = "A" };
        fixture.Db.Utilizadores.Add(owner);
        var trip = new Viagem
        {
            CondutorId = owner.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(2),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3
        };
        fixture.Db.Viagens.Add(trip);
        await fixture.Db.SaveChangesAsync();
        var controller = new ViagensController(fixture.Db, new MapasService(
            new HttpClient(), new ConfigurationBuilder().Build()));
        Authenticate(controller, Guid.NewGuid());
        Assert.IsType<ForbidResult>(await controller.Editar(trip.Id,
            new(DateTime.UtcNow.AddDays(3), 4), default));
    }

    [Fact]
    public async Task CT038_PaginacaoInvalida_Devolve400()
    {
        using var fixture = new DatabaseFixture();
        var controller = new PesquisaController(fixture.Db);
        Assert.IsType<BadRequestObjectResult>(await controller.Pesquisar(
            null, null, null, null, -1, 99999, default));
    }

    [Fact]
    public async Task CT034_PesquisaExcluiViagensInativas()
    {
        using var fixture = new DatabaseFixture();
        var owner = new Utilizador { Email = "a@estg.ipp.pt", Nome = "A" };
        fixture.Db.Utilizadores.Add(owner);
        fixture.Db.Viagens.AddRange(
            new Viagem
            {
                CondutorId = owner.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = DateTime.UtcNow.AddDays(2),
                LugaresOferecidos = 2,
                LugaresDisponiveis = 2
            },
            new Viagem
            {
                CondutorId = owner.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = DateTime.UtcNow.AddDays(2),
                Estado = "Indisponibilizada",
                LugaresOferecidos = 2,
                LugaresDisponiveis = 2
            });
        await fixture.Db.SaveChangesAsync();
        var controller = new PesquisaController(fixture.Db);
        var result = Assert.IsType<OkObjectResult>(await controller.Pesquisar(
            null, null, null, null, 1, 20, default));
        var total = result.Value!.GetType().GetProperty("total")!.GetValue(result.Value);
        Assert.Equal(1, total);
    }

}

public sealed class MapasAndValidationTests
{
    private static EstimativaCustoService CriarCustos()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custos:PrecoPorKm"] = "0.10"
            })
            .Build();

        return new EstimativaCustoService(configuracao);
    }
        
    [Fact]
    public async Task CT032_MapasSemChave_Devolve503()
    {
        var service = new MapasService(new HttpClient(), new ConfigurationBuilder().Build());

        var controller = new RotasController(service, CriarCustos());

        var result = await controller.Calcular(new("Porto", "Braga"), default);

        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task CT030_GeocodificarTextoVazio_Devolve400()
    {
        var service = new MapasService(new HttpClient(), new ConfigurationBuilder().Build());

        var controller = new RotasController(service, CriarCustos());

        Assert.IsType<BadRequestResult>(await controller.Geocodificar(" ", default));
    }

    [Fact]
    public void Custo_DivisaoPorCondutorEMaisUmPassageiro()
    {
        Assert.Equal(2.50m, Domain.Services.CalculadoraDeCusto.Calcular(50m, 0.10m, 1));
    }

    [Fact]
    public void Custo_RejeitaPassageirosNegativos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Domain.Services.CalculadoraDeCusto.Calcular(50m, 0.10m, -1));
    }
}
