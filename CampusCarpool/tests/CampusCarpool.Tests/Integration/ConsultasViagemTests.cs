using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Consultas de viagens (A7 / G20; AD-003 §5.1): quem consulta uma viagem e o que vê.</summary>
/// <remarks>SQLite em memória; o controller é criado diretamente, sem Serviço de Mapas (nada aqui o usa).</remarks>
public sealed class ConsultasViagemTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador condutor = NovoUtilizador("Condutora");
    private readonly Utilizador passageiro = NovoUtilizador("Passageiro");
    private readonly Utilizador pendente = NovoUtilizador("Pendente");
    private readonly Utilizador outro = NovoUtilizador("Outro");

    public ConsultasViagemTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseSqlite(ligacao).Options);
        db.Database.EnsureCreated();
        db.AddRange(condutor, passageiro, pendente, outro);
        db.SaveChanges();
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    private static Utilizador NovoUtilizador(string nome) =>
        new() { Nome = nome, Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };

    private async Task<Viagem> CriarViagem(string estado = "Ativa", int lugaresDisponiveis = 2)
    {
        var viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            DestinoLatitude = 41.55m,
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 3,
            LugaresDisponiveis = lugaresDisponiveis,
            Estado = estado
        };
        db.Viagens.Add(viagem);
        db.Participacoes.AddRange(
            new Participacao { ViagemId = viagem.Id, PassageiroId = passageiro.Id, Estado = EstadoParticipacao.Aceite, Recolha = "Maia" },
            new Participacao { ViagemId = viagem.Id, PassageiroId = pendente.Id, Estado = EstadoParticipacao.Pendente, Destino = "Trofa" });
        await db.SaveChangesAsync();
        return viagem;
    }

    private ViagensController Controller(Guid utilizadorId) => new(db, new MapasFalsos().CriarServico())
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

    [Theory]
    [InlineData("Indisponibilizada", 2)]
    [InlineData("Concluida", 2)]
    [InlineData("Ativa", 0)]
    /// <summary>A7: outro utilizador só consulta uma viagem pesquisável; condutor e quem tem pedido consultam sempre.</summary>
    public async Task I110_46_ViagemNaoPesquisavel_SoParaOCondutorEQuemTemPedido(string estado, int lugares)
    {
        var viagem = await CriarViagem(estado, lugares);

        Assert.IsType<NotFoundResult>(await Controller(outro.Id).Consultar(viagem.Id, default));
        Assert.IsType<OkObjectResult>(await Controller(condutor.Id).Consultar(viagem.Id, default));
        Assert.IsType<OkObjectResult>(await Controller(passageiro.Id).Consultar(viagem.Id, default));
        Assert.IsType<OkObjectResult>(await Controller(pendente.Id).Consultar(viagem.Id, default));
    }

    [Fact]
    public async Task I110_47_ViagemPesquisavel_QualquerUtilizadorConsulta()
    {
        var viagem = await CriarViagem();

        Assert.IsType<OkObjectResult>(await Controller(outro.Id).Consultar(viagem.Id, default));
    }
}
