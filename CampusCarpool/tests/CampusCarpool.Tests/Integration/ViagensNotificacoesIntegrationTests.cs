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

public sealed class ViagensNotificacoesIntegrationTests
{

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            _connection.Open();

            var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(_connection)
                .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.EnsureCreated();
        }

        public async Task<Utilizador> CriarUtilizadorAsync()
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

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }

    private static ViagensController CriarController(CampusCarpoolDbContext db, Guid condutorId)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = "chave-ficticia-apenas-para-testes"
            })
            .Build();

        var mapas = new MapasService(new HttpClient(), configuracao);

        var repositorioNotificacoes = new RepositorioNotificacoes(db);

        var filaPushFalsa = new FilaNotificacoesPushFalsa();

        var notificacaoService = new NotificacaoService(repositorioNotificacoes, filaPushFalsa);

        var controller = new ViagensController(db, mapas, notificacaoService: notificacaoService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
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
    public async Task EditarViagem_NotificaPassageirosAceitesMasNaoPendentes()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizadorAsync();
        var passageiroAceite = await f.CriarUtilizadorAsync();
        var passageiroPendente = await f.CriarUtilizadorAsync();

        var viagem = new Viagem
        {
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
            new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiroAceite.Id,
                Estado = EstadoParticipacao.Aceite
            },
            new Participacao
            {
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

        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == passageiroPendente.Id);
    }

    [Fact]
    public async Task IndisponibilizarViagem_CancelaENotificaPassageirosAceitesEPendentes()
    {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizadorAsync();
        var passageiroAceite = await f.CriarUtilizadorAsync();
        var passageiroPendente = await f.CriarUtilizadorAsync();

        var viagem = new Viagem
        {
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
            new Participacao
            {
                ViagemId = viagem.Id,
                PassageiroId = passageiroAceite.Id,
                Estado = EstadoParticipacao.Aceite
            },
            new Participacao
            {
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


        // P-25 / RF-F002-10 (DEF-BE-008, I110): o pedido pendente e a participação aceite
        // ficam canceladas e todos os lugares ficam livres.
        var participacoes = await f.Db.Participacoes
            .AsNoTracking()
            .Where(p => p.ViagemId == viagem.Id)
            .ToListAsync();

        Assert.Equal(2, participacoes.Count);
        Assert.All(participacoes, p => Assert.Equal(EstadoParticipacao.Cancelada, p.Estado));
        Assert.Equal(3, viagemAtualizada.LugaresDisponiveis);

        // P-23 / RF-F006-03 (DEF-BE-020, I110): uma notificação Alteração da viagem por
        // passageiro afetado (aceite e pendente) e nenhuma para o condutor.
        var notificacoes = await f.Db.Notificacoes
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, notificacoes.Count);
        Assert.Equal(
            new[] { passageiroAceite.Id, passageiroPendente.Id }.Order(),
            notificacoes.Select(n => n.UtilizadorId).Order());
        Assert.All(notificacoes, n =>
        {
            Assert.Equal(TipoNotificacao.AlteracaoViagem, n.Tipo);
            Assert.Contains("indisponível", n.Conteudo);
        });
        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == condutor.Id);
    }

}
