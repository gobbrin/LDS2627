using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
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

    private static readonly IServicoMapasRotas Mapas = new MapasFalsos().CriarServico(configurado: false);

    private ViagensController Controller(Guid utilizadorId) => new(ServicosDeTeste.CriarViagens(db, Mapas))
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

    private static T Campo<T>(object? resposta, string nome) =>
        (T)resposta!.GetType().GetProperty(nome)!.GetValue(resposta)!;

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

        var pesquisa = Assert.IsType<OkObjectResult>(await ServicosDeTeste.CriarPesquisaController(db).Pesquisar(null, null, null, null)).Value!;
        var resultado = Assert.Single((System.Collections.IEnumerable)pesquisa.GetType().GetProperty("viagens")!.GetValue(pesquisa)!);
        Assert.Equal(condutorDetalhes, Campo<CondutorViagem>(resultado, "Condutor"));
        Assert.Equal(1, Campo<int>(resultado, "PontosAdicionais"));
    }

    [Fact]
    /// <summary>RF-F003-07 / DEF-BE-012, P-13: só o condutor e os aceites veem os passageiros e os pontos.</summary>
    public async Task I110_49_Passageiros_SoParaOCondutorEOsAceites()
    {
        var viagem = await CriarViagem();

        var lista = Assert.IsType<OkObjectResult>(await Controller(condutor.Id).ListarPassageiros(viagem.Id, default)).Value!;
        var passageiroListado = Assert.Single((System.Collections.IEnumerable)lista);
        Assert.Equal("Passageiro", Campo<string>(passageiroListado, "Nome"));
        Assert.Equal("Maia", Campo<string>(passageiroListado, "Recolha"));

        Assert.IsType<OkObjectResult>(await Controller(passageiro.Id).ListarPassageiros(viagem.Id, default));
        Assert.IsType<ForbidResult>(await Controller(pendente.Id).ListarPassageiros(viagem.Id, default));
        Assert.IsType<ForbidResult>(await Controller(outro.Id).ListarPassageiros(viagem.Id, default));
        Assert.IsType<NotFoundResult>(await Controller(condutor.Id).ListarPassageiros(Guid.NewGuid(), default));
    }

    [Fact]
    /// <summary>RF-F004-08 / DEF-BE-016: os pedidos pendentes da viagem, só para o condutor.</summary>
    public async Task I110_50_PedidosPendentes_SoParaOCondutor()
    {
        var viagem = await CriarViagem();

        var lista = Assert.IsType<OkObjectResult>(await Controller(condutor.Id).ListarPedidosPendentes(viagem.Id, default)).Value!;
        var pedido = Assert.Single((System.Collections.IEnumerable)lista);
        Assert.Equal("Pendente", Campo<string>(pedido, "Nome"));
        Assert.Equal("Trofa", Campo<string>(pedido, "Destino"));

        Assert.IsType<ForbidResult>(await Controller(passageiro.Id).ListarPedidosPendentes(viagem.Id, default));
        Assert.IsType<NotFoundResult>(await Controller(condutor.Id).ListarPedidosPendentes(Guid.NewGuid(), default));
    }

    [Fact]
    /// <summary>RF-F002-15 / DEF-BE-010: só as viagens ativas do próprio condutor, com os lugares disponíveis.</summary>
    public async Task I110_51_MinhasViagens_SoAsAtivasDoCondutor()
    {
        var ativa = await CriarViagem(lugaresDisponiveis: 1);
        await CriarViagem("Indisponibilizada");

        var lista = Assert.IsType<OkObjectResult>(await Controller(condutor.Id).ListarMinhas(default)).Value!;
        var viagem = Assert.Single((System.Collections.IEnumerable)lista);
        Assert.Equal(ativa.Id, Campo<Guid>(viagem, "Id"));
        Assert.Equal(1, Campo<int>(viagem, "LugaresDisponiveis"));

        Assert.Empty((System.Collections.IEnumerable)Assert.IsType<OkObjectResult>(
            await Controller(outro.Id).ListarMinhas(default)).Value!);
    }

    [Theory]
    [InlineData("Famalicao", null)]
    [InlineData("FAMALICÃO", null)]
    [InlineData("famalicão", null)]
    [InlineData(null, "acores")]
    [InlineData(null, "AÇORES")]
    /// <summary>P-12 / D-009 (B4): a pesquisa não distingue maiúsculas nem acentos.</summary>
    public async Task I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(string? origem, string? destino)
    {
        db.Viagens.Add(new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Vila Nova de Famalicão",
            Destino = "Ponta Delgada, Açores",
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3
        });
        await db.SaveChangesAsync();

        var resposta = Assert.IsType<OkObjectResult>(await ServicosDeTeste.CriarPesquisaController(db).Pesquisar(origem, destino, null, null)).Value!;

        Assert.Equal(1, Campo<int>(resposta, "total"));
    }

    [Fact]
    /// <summary>P-12: a alteração de uma viagem atualiza o texto usado na pesquisa.</summary>
    public async Task I110_53_AlterarOrigem_AtualizaOTextoDePesquisa()
    {
        var viagem = await CriarViagem();
        viagem.Origem = "Guimarães";
        await db.SaveChangesAsync();

        var resposta = Assert.IsType<OkObjectResult>(await ServicosDeTeste.CriarPesquisaController(db).Pesquisar("guimaraes", null, null, null)).Value!;

        Assert.Equal(1, Campo<int>(resposta, "total"));
    }

    [Fact]
    /// <summary>RF-F005-08 / DEF-BE-018: uma entrada por interlocutor, com a última mensagem, e nada de terceiros.</summary>
    public async Task I110_58_ListaDeConversas_UmaEntradaPorInterlocutor()
    {
        var agora = DateTime.UtcNow;
        db.Mensagens.AddRange(
            new Mensagem { RemetenteId = condutor.Id, DestinatarioId = passageiro.Id, Conteudo = "primeira", EnviadoEm = agora.AddMinutes(-30) },
            new Mensagem { RemetenteId = passageiro.Id, DestinatarioId = condutor.Id, Conteudo = "resposta", EnviadoEm = agora.AddMinutes(-20) },
            new Mensagem { RemetenteId = pendente.Id, DestinatarioId = condutor.Id, Conteudo = "mais recente", EnviadoEm = agora.AddMinutes(-10) },
            new Mensagem { RemetenteId = pendente.Id, DestinatarioId = outro.Id, Conteudo = "de terceiros", EnviadoEm = agora });
        await db.SaveChangesAsync();

        var conversas = await new CampusCarpool.Infrastructure.Repositories.RepositorioMensagens(db)
            .ListarConversasAsync(condutor.Id, default);

        Assert.Equal(2, conversas.Count);
        Assert.Equal("Pendente", conversas[0].OutroUtilizadorNome);
        Assert.Equal("mais recente", conversas[0].UltimaMensagem);
        Assert.Equal("Passageiro", conversas[1].OutroUtilizadorNome);
        Assert.Equal("resposta", conversas[1].UltimaMensagem);
    }

    [Fact]
    public async Task I110_47_ViagemPesquisavel_QualquerUtilizadorConsulta()
    {
        var viagem = await CriarViagem();

        Assert.IsType<OkObjectResult>(await Controller(outro.Id).Consultar(viagem.Id, default));
    }
}
