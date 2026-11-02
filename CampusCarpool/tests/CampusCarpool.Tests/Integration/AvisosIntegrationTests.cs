using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Testes da comunicação do condutor e das notificações persistidas da I044.</summary>
public sealed class AvisosIntegrationTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(connection)
                .Options);
            Db.Database.EnsureCreated();
        }

        public async Task<Utilizador> CriarUtilizador()
        {
            var utilizador = new Utilizador
            {
                Nome = "Utilizador de teste",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };
            Db.Utilizadores.Add(utilizador);
            await Db.SaveChangesAsync();
            return utilizador;
        }

        public async Task<Viagem> CriarViagem(Guid condutorId)
        {
            var viagem = new Viagem
            {
                CondutorId = condutorId,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = DateTime.UtcNow.AddDays(2),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            };
            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();
            return viagem;
        }

        public async Task<Participacao> CriarParticipacao(
            Guid viagemId,
            Guid passageiroId,
            EstadoParticipacao estado)
        {
            var participacao = new Participacao
            {
                ViagemId = viagemId,
                PassageiroId = passageiroId,
                Estado = estado
            };
            Db.Participacoes.Add(participacao);
            await Db.SaveChangesAsync();
            return participacao;
        }

        public void Dispose()
        {
            Db.Dispose();
            connection.Dispose();
        }
    }

    private static AvisosController CriarAvisosController(
        CampusCarpoolDbContext db,
        Guid? utilizadorId)
    {
        var controller = new AvisosController(new AvisoService(db));
        AplicarIdentidade(controller, utilizadorId);
        return controller;
    }

    private static NotificacoesController CriarNotificacoesController(
        CampusCarpoolDbContext db,
        Guid? utilizadorId)
    {
        var repositorioNotificacoes = new RepositorioNotificacoes(db);
        var filaPushFalsa = new FilaNotificacoesPushFalsa();

        var notificacaoService = new NotificacaoService(repositorioNotificacoes, filaPushFalsa);
    

        var controller = new NotificacoesController(notificacaoService);
        AplicarIdentidade(controller, utilizadorId);

        return controller;
    }

    private static void AplicarIdentidade(ControllerBase controller, Guid? utilizadorId)
    {
        var claims = utilizadorId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, utilizadorId.Value.ToString()) }
            : Array.Empty<Claim>();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    claims,
                    utilizadorId.HasValue ? "Test" : null))
            }
        };
    }

    [Fact]
    public async Task CondutorCriaAviso_NotificaApenasPassageirosAceites()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiroAceite = await f.CriarUtilizador();
        var passageiroPendente = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);
        await f.CriarParticipacao(viagem.Id, passageiroAceite.Id, EstadoParticipacao.Aceite);
        await f.CriarParticipacao(viagem.Id, passageiroPendente.Id, EstadoParticipacao.Pendente);

        var resultado = await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new("A partida será antecipada."), default);

        var resposta = Assert.IsType<CreatedResult>(resultado);
        Assert.Equal(201, resposta.StatusCode);
        var aviso = Assert.Single(await f.Db.Avisos.ToListAsync());
        var notificacao = Assert.Single(await f.Db.Notificacoes.ToListAsync());
        Assert.Equal(viagem.Id, aviso.ViagemId);
        Assert.Equal(condutor.Id, aviso.CondutorId);
        Assert.Equal(passageiroAceite.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.Aviso, notificacao.Tipo);
        Assert.Equal(aviso.Conteudo, notificacao.Conteudo);
    }

    [Fact]
    public async Task OutroUtilizadorNaoPodeCriarAviso()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarAvisosController(f.Db, outroUtilizador.Id).Criar(
            viagem.Id, new("Mensagem não autorizada"), default);

        Assert.IsType<ForbidResult>(resultado);
        Assert.Empty(f.Db.Avisos);
        Assert.Empty(f.Db.Notificacoes);
    }

    [Fact]
    public async Task ViagemInexistenteNaoCriaAviso()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();

        var resultado = await CriarAvisosController(f.Db, condutor.Id).Criar(
            Guid.NewGuid(), new("Mensagem"), default);

        Assert.IsType<NotFoundResult>(resultado);
        Assert.Empty(f.Db.Avisos);
    }

    [Fact]
    public async Task ConteudoComCemCaracteres_EValido()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new(new string('A', 100)), default);

        Assert.IsType<CreatedResult>(resultado);
    }

    [Fact]
    public async Task ConteudoComMaisDeCemCaracteres_EInvalido()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new(new string('A', 101)), default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(f.Db.Avisos);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConteudoInvalido_NaoCriaAviso(string? conteudo)
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new(conteudo), default);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(f.Db.Avisos);
        Assert.Empty(f.Db.Notificacoes);
    }

    [Fact]
    public async Task PassageiroAceitePodeConsultarAvisos()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);
        await f.CriarParticipacao(viagem.Id, passageiro.Id, EstadoParticipacao.Aceite);
        await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new("Primeiro aviso"), default);

        var resultado = await CriarAvisosController(f.Db, passageiro.Id).Listar(viagem.Id, default);

        var resposta = Assert.IsType<OkObjectResult>(resultado);
        Assert.Single((System.Collections.IEnumerable)resposta.Value!);
    }

    [Fact]
    public async Task PassageiroPendenteNaoPodeConsultarAvisos()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);
        await f.CriarParticipacao(viagem.Id, passageiro.Id, EstadoParticipacao.Pendente);
        await CriarAvisosController(f.Db, condutor.Id).Criar(
            viagem.Id, new("Aviso privado"), default);

        var resultado = await CriarAvisosController(f.Db, passageiro.Id).Listar(viagem.Id, default);

        Assert.IsType<ForbidResult>(resultado);
    }

    [Fact]
    public async Task UtilizadorNaoRelacionadoNaoPodeConsultarAvisos()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);

        var resultado = await CriarAvisosController(f.Db, outroUtilizador.Id).Listar(viagem.Id, default);

        Assert.IsType<ForbidResult>(resultado);
    }

    [Fact]
    public async Task NotificacoesSaoPessoaisEOrdenadasPorChegada()
    {
        using var f = new Fixture();
        var utilizador = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();
        f.Db.Notificacoes.AddRange(
            new Notificacao { UtilizadorId = utilizador.Id, Tipo = TipoNotificacao.Aviso, Conteudo = "Antiga", CriadaEm = DateTime.UtcNow.AddMinutes(-2) },
            new Notificacao { UtilizadorId = utilizador.Id, Tipo = TipoNotificacao.NovaMensagem, Conteudo = "Recente", CriadaEm = DateTime.UtcNow },
            new Notificacao { UtilizadorId = outroUtilizador.Id, Tipo = TipoNotificacao.Aviso, Conteudo = "De outro", CriadaEm = DateTime.UtcNow.AddMinutes(1) });
        await f.Db.SaveChangesAsync();

        var resultado = await CriarNotificacoesController(f.Db, utilizador.Id).Listar(1, 20, default);

        var pagina = Assert.IsType<OkObjectResult>(resultado).Value as PaginaNotificacoes;
        Assert.NotNull(pagina);
        Assert.Equal(2, pagina.Total);
        Assert.Equal(2, pagina.NaoLidas);
        Assert.Equal("Recente", pagina.Itens[0].Conteudo);
        Assert.Equal("Antiga", pagina.Itens[1].Conteudo);
    }

    [Fact]
    public async Task NotificacoesSemIdentidade_Devolve401()
    {
        using var f = new Fixture();

        var resultado = await CriarNotificacoesController(f.Db, null).Listar(1, 20, default);

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task NotificacoesSuportamPaginacao()
    {
        using var f = new Fixture();
        var utilizador = await f.CriarUtilizador();
        for (var indice = 1; indice <= 3; indice++)
        {
            f.Db.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = utilizador.Id,
                Tipo = TipoNotificacao.Aviso,
                Conteudo = $"Aviso {indice}",
                CriadaEm = DateTime.UtcNow.AddMinutes(-indice)
            });
        }
        await f.Db.SaveChangesAsync();

        var resultado = await CriarNotificacoesController(f.Db, utilizador.Id).Listar(2, 2, default);

        var pagina = Assert.IsType<PaginaNotificacoes>(Assert.IsType<OkObjectResult>(resultado).Value);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(3, pagina.Total);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Single(pagina.Itens);
    }

    [Fact]
    public async Task NotificacaoDoProprioUtilizadorPodeSerMarcadaComoLida()
    {
        using var f = new Fixture();

        var utilizador = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();

        var notificacao = new Notificacao
        {
            UtilizadorId = utilizador.Id,
            Tipo = TipoNotificacao.Aviso,
            Conteudo = "Notificação privada"
        };

        f.Db.Notificacoes.Add(notificacao);
        await f.Db.SaveChangesAsync();

        var resultado = await CriarNotificacoesController(f.Db, utilizador.Id)
            .MarcarComoLida(notificacao.Id, default);

        Assert.IsType<NoContentResult>(resultado);

        var notificacaoPersistida = await f.Db.Notificacoes
            .AsNoTracking()
            .SingleAsync(n => n.Id == notificacao.Id);

        Assert.NotNull((notificacaoPersistida.LidaEm));
    }

    [Fact]
    public async Task UtilizadorNaoPodeMarcarNotificacaoDeOutroComoLida()
    {
        using var f = new Fixture();
        var utilizador = await f.CriarUtilizador();
        var outro = await f.CriarUtilizador();
        var notificacao = new Notificacao
        {
            UtilizadorId = outro.Id,
            Tipo = TipoNotificacao.Aviso,
            Conteudo = "Privado"
        };
        f.Db.Notificacoes.Add(notificacao);
        await f.Db.SaveChangesAsync();

        var resultado = await CriarNotificacoesController(f.Db, utilizador.Id)
            .MarcarComoLida(notificacao.Id, default);

        Assert.IsType<NotFoundResult>(resultado);
        Assert.Null((await f.Db.Notificacoes.FindAsync(notificacao.Id))!.LidaEm);
    }

    
    [Fact]
    public async Task UtilizadorPodeMarcarTodasAsNotificacoesComoLidas() {
        using var f = new Fixture();
        var utilizador = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();

        f.Db.Notificacoes.AddRange(
            new Notificacao {
                UtilizadorId = utilizador.Id,
                Tipo = TipoNotificacao.Aviso,
                Conteudo = "Primeiro aviso"
            },
            new Notificacao {
                UtilizadorId = utilizador.Id,
                Tipo = TipoNotificacao.NovaMensagem,
                Conteudo = "Nova mensagem"
            },
            new Notificacao {
                UtilizadorId = outroUtilizador.Id,
                Tipo = TipoNotificacao.Aviso,
                Conteudo = "Aviso de outro utilizador"
            });

        await f.Db.SaveChangesAsync();

        var controller = CriarNotificacoesController(f.Db, utilizador.Id);

        var resultado = await controller.MarcarTodasComoLidas(default);

        Assert.IsType<OkObjectResult>(resultado);

        var notificacoes = await f.Db.Notificacoes
            .AsNoTracking()
            .ToListAsync();

        Assert.All(
            notificacoes.Where(n => n.UtilizadorId == utilizador.Id),
            notificacao => Assert.NotNull(notificacao.LidaEm));

        var notificacaoDoOutro = Assert.Single(notificacoes.Where(n => n.UtilizadorId == outroUtilizador.Id));

        Assert.Null(notificacaoDoOutro.LidaEm);
    }

        
    [Fact]
    public async Task MarcarTodasComoLidasSemIdentidade_Devolve401() {
        using var f = new Fixture();

        var resultado = await CriarNotificacoesController(f.Db, null)
            .MarcarTodasComoLidas(default);

        Assert.IsType<UnauthorizedResult>(resultado);
    }


}
