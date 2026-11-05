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

    private static T Campo<T>(object resposta, string nome) =>
        (T)resposta.GetType().GetProperty(nome)!.GetValue(resposta)!;

    private async Task<Viagem> CriarViagemComRotaEAvaliacoes()
    {
        var viagem = await CriarViagem();
        var outraViagem = new Viagem { CondutorId = condutor.Id, Origem = "A", Destino = "B", PartidaEm = DateTime.UtcNow.AddDays(-5), Estado = "Concluida" };
        db.Viagens.Add(outraViagem);
        db.Avaliacoes.AddRange(
            new Avaliacao { ViagemId = outraViagem.Id, AvaliadorId = passageiro.Id, AvaliadoId = condutor.Id, Classificacao = 5 },
            new Avaliacao { ViagemId = outraViagem.Id, AvaliadorId = pendente.Id, AvaliadoId = condutor.Id, Classificacao = 4 });

        var origem = new PontoRota(null, TiposPontoRota.Origem, "Porto", new Coordenadas(41.15m, -8.61m));
        var recolha = new PontoRota(Guid.NewGuid(), TiposPontoRota.Recolha, "Maia", new Coordenadas(41.23m, -8.62m));
        var destino = new PontoRota(null, TiposPontoRota.DestinoViagem, "Braga", new Coordenadas(41.55m, -8.42m));
        viagem.RotaPontos = RotaGuardada.Serializar(new ResultadoRotaComPontos(
            [origem, recolha, destino],
            [new SegmentoRota(origem, recolha, 10m, 10), new SegmentoRota(recolha, destino, 20m, 20)],
            30m,
            30));
        await db.SaveChangesAsync();
        return viagem;
    }

    [Fact]
    /// <summary>RF-F003-03, RF-F003-04, RF-F003-06 / DEF-BE-011: nome e média do condutor e número de pontos adicionais.</summary>
    public async Task I110_48_DetalhesEPesquisa_TrazemOCondutorEOsPontosAdicionais()
    {
        var viagem = await CriarViagemComRotaEAvaliacoes();

        var detalhes = Assert.IsType<OkObjectResult>(await Controller(outro.Id).Consultar(viagem.Id, default)).Value!;
        var condutorDetalhes = Campo<CondutorViagem>(detalhes, "Condutor");
        Assert.Equal("Condutora", condutorDetalhes.Nome);
        Assert.Equal(4.5m, condutorDetalhes.ClassificacaoMedia);
        Assert.Equal(1, Campo<int>(detalhes, "PontosAdicionais"));

        var pesquisa = Assert.IsType<OkObjectResult>(await new PesquisaController(db).Pesquisar(null, null, null, null)).Value!;
        var resultado = Assert.Single((System.Collections.IEnumerable)pesquisa.GetType().GetProperty("viagens")!.GetValue(pesquisa)!);
        Assert.Equal(condutorDetalhes, Campo<CondutorViagem>(resultado, "Condutor"));
        Assert.Equal(1, Campo<int>(resultado, "PontosAdicionais"));
    }

    [Fact]
    public async Task I110_47_ViagemPesquisavel_QualquerUtilizadorConsulta()
    {
        var viagem = await CriarViagem();

        Assert.IsType<OkObjectResult>(await Controller(outro.Id).Consultar(viagem.Id, default));
    }
}
