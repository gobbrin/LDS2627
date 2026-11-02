using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusCarpool.Tests.Integration;

public sealed class ViagensNotificacoesIntegrationTests {

    private sealed class Fixture : IDisposable {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Fixture() {
            _connection.Open();

            var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(_connection)
                .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.EnsureCreated();
        }

        public async Task<Utilizador> CriarUtilizadorAsync() {
            var utilizador = new Utilizador {
                Nome = "Utilizador de teste",
                Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
                EmailConfirmado = true
            };

            Db.Utilizadores.Add(utilizador);
            await Db.SaveChangesAsync();

            return utilizador;
        }

        public void Dispose() {
            Db.Dispose();
            _connection.Dispose();
        }
    }

    private static ViagensController CriarController(CampusCarpoolDbContext db, Guid condutorId) {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Maps:ApiKey"] = "chave-ficticia-apenas-para-testes"
            })
            .Build();

        var mapas = new MapasService(new HttpClient(), configuracao);

        var repositorioNotificacoes = new RepositorioNotificacoes(db);

        var filaPushFalsa = new FilaNotificacoesPushFalsa();

        var notificacaoService = new NotificacaoService(repositorioNotificacoes, filaPushFalsa);

        var controller = new ViagensController(db, mapas, notificacaoService: notificacaoService);

        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                condutorId.ToString())
                        },
                        "Test"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task EditarViagem_NotificaPassageirosAceitesMasNaoPendentes() {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizadorAsync();
        var passageiroAceite = await f.CriarUtilizadorAsync();
        var passageiroPendente = await f.CriarUtilizadorAsync();

        var viagem = new Viagem {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(3),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 2,
            Estado = "Ativa"
        };

        f.Db.Viagens.Add(viagem);

        f.Db.Participacoes.AddRange(
            new Participacao {
                ViagemId = viagem.Id,
                PassageiroId = passageiroAceite.Id,
                Estado = EstadoParticipacao.Aceite
            },
            new Participacao {
                ViagemId = viagem.Id,
                PassageiroId = passageiroPendente.Id,
                Estado = EstadoParticipacao.Pendente
            });

        await f.Db.SaveChangesAsync();

        var controller = CriarController(f.Db, condutor.Id);

        var resultado = await controller.Editar(
            viagem.Id,
            new ViagensController.EditarViagem(
                DateTime.UtcNow.AddDays(4),
                3),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var notificacoes = await f.Db.Notificacoes.ToListAsync();

        var notificacao = Assert.Single(notificacoes);

        Assert.Equal(passageiroAceite.Id, notificacao.UtilizadorId);

        Assert.Equal(TipoNotificacao.AlteracaoViagem, notificacao.Tipo);

        Assert.DoesNotContain( notificacoes, n => n.UtilizadorId == passageiroPendente.Id);
    }

    [Fact]
    public async Task IndisponibilizarViagem_NotificaPassageirosAceitesMasNaoPendentes() {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizadorAsync();
        var passageiroAceite = await f.CriarUtilizadorAsync();
        var passageiroPendente = await f.CriarUtilizadorAsync();

        var viagem = new Viagem {
            CondutorId = condutor.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(3),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 2,
            Estado = "Ativa"
        };

        f.Db.Viagens.Add(viagem);

        f.Db.Participacoes.AddRange(
            new Participacao {
                ViagemId = viagem.Id,
                PassageiroId = passageiroAceite.Id,
                Estado = EstadoParticipacao.Aceite
            },
            new Participacao {
                ViagemId = viagem.Id,
                PassageiroId = passageiroPendente.Id,
                Estado = EstadoParticipacao.Pendente
            });

        await f.Db.SaveChangesAsync();

        var controller = CriarController(f.Db, condutor.Id);

        var resultado = await controller.Indisponibilizar(viagem.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        //Confirma que a viagem ficou indisponível.
        var viagemAtualizada = await f.Db.Viagens
            .AsNoTracking()
            .SingleAsync(v => v.Id == viagem.Id);

        Assert.Equal("Indisponibilizada", viagemAtualizada.Estado);

        //Confirma que foi criada uma notificação para o passageiro aceite.
        var notificacoes = await f.Db.Notificacoes
            .AsNoTracking()
            .ToListAsync();

        var notificacao = Assert.Single(notificacoes);

        Assert.Equal(passageiroAceite.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.AlteracaoViagem, notificacao.Tipo);
        Assert.Contains("indisponível", notificacao.Conteudo);

        //O pedido pendente não deve receber esta notificação.
        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == passageiroPendente.Id);

        //Confirma que a participação pendente foi cancelada.
        var participacaoPendente = await f.Db.Participacoes
            .AsNoTracking()
            .SingleAsync(p => p.PassageiroId == passageiroPendente.Id);

        Assert.Equal(EstadoParticipacao.Cancelada, participacaoPendente.Estado);
    }

}
