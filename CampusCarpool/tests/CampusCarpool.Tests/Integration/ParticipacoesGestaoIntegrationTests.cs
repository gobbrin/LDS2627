using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Application.Services;

namespace CampusCarpool.Tests.Integration;
/// <summary>
/// Testes de integração para a gestão de participações em viagens, incluindo aceitação, rejeição e cancelamento de pedidos.
/// </summary>
/// <remarks>
/// Estes testes utilizam uma base de dados SQLite em memória para isolar cada cenário e verificar o comportamento do sistema em condições controladas.
/// 
/// </remarks>  
public sealed class ParticipacoesGestaoIntegrationTests
{
    private sealed class NotificadorPedidosTempoRealFalso : INotificadorPedidosTempoReal
    {
        public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;

        public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;

        public Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CampusCarpoolDbContext Db { get; }
        public class ServicoPushFalso : System.Reflection.DispatchProxy
        {
            protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
            {
                var returnType = targetMethod?.ReturnType;
                if (returnType == typeof(Task))
                    return Task.CompletedTask;

                if (returnType?.IsGenericType == true && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];
                    var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!
                        .MakeGenericMethod(resultType)
                        .Invoke(null, new[] { result });
                }

                return returnType?.IsValueType == true ? Activator.CreateInstance(returnType) : null;
            }
        }

        /// <summary>Cria uma base SQLite em memória para isolar cada cenário.</summary>
        public Fixture()
        {
            connection.Open();
            Db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                .UseSqlite(connection)
                .Options);
            Db.Database.EnsureCreated();
        }

        /// <summary>Cria um utilizador confirmado para os cenários de participação.</summary>
        /// <returns>O utilizador criado.</returns>
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

        /// <summary>Cria uma viagem futura com capacidade controlada pelo teste.</summary>
        public async Task<Viagem> CriarViagem(
            Guid condutorId,
            int lugaresOferecidos = 2,
            int? lugaresDisponiveis = null,
            DateTime? partida = null,
            string estado = "Ativa")
        {
            var viagem = new Viagem
            {
                CondutorId = condutorId,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = partida ?? DateTime.UtcNow.AddDays(2),
                LugaresOferecidos = lugaresOferecidos,
                LugaresDisponiveis = lugaresDisponiveis ?? lugaresOferecidos,
                Estado = estado
            };
            Db.Viagens.Add(viagem);
            await Db.SaveChangesAsync();
            return viagem;
        }

        /// <summary>Cria uma participação no estado indicado para preparar um cenário.</summary>
        public async Task<Participacao> CriarParticipacao(
            Guid viagemId,
            Guid passageiroId,
            EstadoParticipacao estado = EstadoParticipacao.Pendente)
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

        /// <summary>Liberta a base de dados SQLite usada pelo cenário.</summary>
        public void Dispose()
        {
            Db.Dispose();
            connection.Dispose();
        }
    }

    /// <summary>Cria o controller com o utilizador autenticado simulado no claim JWT.</summary>
  private static GestaoParticipacoesController CriarController(
    CampusCarpoolDbContext db,
    Guid? utilizadorId,
    IServicoPush? push = null)
    {
        var repositorioNotificacoes = new RepositorioNotificacoes(db);
        var servicoPush = push ?? new CampusCarpool.Tests.ServicoPushFalso();
var notificacaoService = new NotificacaoService(
    repositorioNotificacoes, servicoPush);
        var repositorioParticipacoes = new RepositorioParticipacoes(db);
        var repositorioViagens = new RepositorioViagens(db);
        var unidadeDeTrabalho = new UnidadeDeTrabalhoEfCore(db);

        var participacaoService = new ParticipacaoService(
            repositorioParticipacoes,
            repositorioViagens,
            unidadeDeTrabalho,
            notificacaoService,
            new NotificadorPedidosTempoRealFalso());

        var controller = new GestaoParticipacoesController(participacaoService);

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
        return controller;
    }

    [Fact]
    /// <summary>Verifica que o condutor aceita um pedido e perde um lugar disponível.</summary>
    public async Task AceitacaoValida_AlteraEstadoEReduzLugar()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 2, lugaresDisponiveis: 2);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);

        var resultado = await CriarController(f.Db, condutor.Id).Aceitar(participacao.Id, default);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(EstadoParticipacao.Aceite, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(1, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);

        //Confirma que o passageiro recebeu a notificação de aceitação.
        var notificacao = Assert.Single(await f.Db.Notificacoes.ToListAsync());

        Assert.Equal(passageiro.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.PedidoAceite, notificacao.Tipo);
    }

    [Fact]
    /// <summary>Verifica que rejeitar um pedido pendente não altera lugares.</summary>
    public async Task RejeicaoValida_AlteraEstadoSemAlterarLugares()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresDisponiveis: 2);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);

        var resultado = await CriarController(f.Db, condutor.Id).Rejeitar(participacao.Id, default);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(EstadoParticipacao.Rejeitada, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(2, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);

        //Confirma que o passageiro recebeu a notificação de rejeição.
        var notificacao = Assert.Single(await f.Db.Notificacoes.ToListAsync());

        Assert.Equal(passageiro.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.PedidoRejeitado, notificacao.Tipo);
    }

    [Fact]
    /// <summary>Verifica que um pedido pendente pode ser cancelado depois da partida.</summary>
    public async Task CancelamentoPendente_EPermitidoMesmoDepoisDaPartida()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, partida: DateTime.UtcNow.AddMinutes(-1));
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);

        var resultado = await CriarController(f.Db, passageiro.Id).Cancelar(participacao.Id, default);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(EstadoParticipacao.Cancelada, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(2, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);
    }

    [Fact]
    /// <summary>Verifica que cancelar uma participação aceite antes da partida liberta um lugar.</summary>
    public async Task CancelamentoAceiteAntesDaPartida_LibertaLugar()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 2, lugaresDisponiveis: 1);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id, EstadoParticipacao.Aceite);

        var resultado = await CriarController(f.Db, passageiro.Id).Cancelar(participacao.Id, default);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(EstadoParticipacao.Cancelada, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(2, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);
    }

    [Fact]
    /// <summary>Verifica que uma participação aceite não pode ser cancelada depois da partida.</summary>
    public async Task CancelamentoAceiteDepoisDaPartida_ERejeitadoENaoAlteraDados()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 2, lugaresDisponiveis: 1,
            partida: DateTime.UtcNow.AddMinutes(-1));
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id, EstadoParticipacao.Aceite);

        var resultado = await CriarController(f.Db, passageiro.Id).Cancelar(participacao.Id, default);

        Assert.IsType<ConflictResult>(resultado);
        Assert.Equal(EstadoParticipacao.Aceite, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(1, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);
    }

    [Fact]
    /// <summary>Verifica que não é possível aceitar sem capacidade disponível.</summary>
    public async Task AceitacaoSemCapacidade_ERejeitadaENaoAlteraPedido()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 1, lugaresDisponiveis: 0);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);

        var resultado = await CriarController(f.Db, condutor.Id).Aceitar(participacao.Id, default);

        Assert.IsType<ConflictObjectResult>(resultado);
        Assert.Equal(EstadoParticipacao.Pendente, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
        Assert.Equal(0, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);
    }

    [Fact]
    /// <summary>Verifica que uma capacidade incoerente falha sem ser mascarada ou corrigida.</summary>
    public async Task CapacidadeInconsistente_ERejeitadaSemCorrigirSilenciosamente()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiroAceite = await f.CriarUtilizador();
        var passageiroPendente = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 3, lugaresDisponiveis: 3);
        await f.CriarParticipacao(viagem.Id, passageiroAceite.Id, EstadoParticipacao.Aceite);
        var pendente = await f.CriarParticipacao(viagem.Id, passageiroPendente.Id);

        var resultado = await CriarController(f.Db, condutor.Id).Aceitar(pendente.Id, default);

        Assert.Equal(409, Assert.IsType<ConflictObjectResult>(resultado).StatusCode);
        Assert.Equal(EstadoParticipacao.Pendente, (await f.Db.Participacoes.FindAsync(pendente.Id))!.Estado);
        Assert.Equal(3, (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);
    }

    [Fact]
    /// <summary>Verifica a autorização do condutor, passageiro e utilizadores externos.</summary>
    public async Task UtilizadorNaoAutorizado_NaoPodeAceitarRejeitarOuCancelar()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var outroUtilizador = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);

        var aceitar = await CriarController(f.Db, outroUtilizador.Id).Aceitar(participacao.Id, default);
        var rejeitar = await CriarController(f.Db, outroUtilizador.Id).Rejeitar(participacao.Id, default);
        var cancelar = await CriarController(f.Db, outroUtilizador.Id).Cancelar(participacao.Id, default);

        Assert.IsType<ForbidResult>(aceitar);
        Assert.IsType<ForbidResult>(rejeitar);
        Assert.IsType<ForbidResult>(cancelar);
        Assert.Equal(EstadoParticipacao.Pendente, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
    }

    [Fact]
    /// <summary>Verifica que o passageiro não pode aceitar nem rejeitar o próprio pedido.</summary>
    public async Task PassageiroNaoPodeAceitarNemRejeitar()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id);
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);
        var controller = CriarController(f.Db, passageiro.Id);

        Assert.IsType<ForbidResult>(await controller.Aceitar(participacao.Id, default));
        Assert.IsType<ForbidResult>(await controller.Rejeitar(participacao.Id, default));
    }

    [Fact]
    /// <summary>Verifica que estados finais ou já aceites não permitem transições inválidas.</summary>
    public async Task TransicoesInvalidas_DevolvemConflito()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, lugaresDisponiveis: 1);
        var aceite = await f.CriarParticipacao(viagem.Id, passageiro.Id, EstadoParticipacao.Aceite);
        var passageiroRejeitado = await f.CriarUtilizador();
        var passageiroCancelado = await f.CriarUtilizador();
        var rejeitada = await f.CriarParticipacao(viagem.Id, passageiroRejeitado.Id, EstadoParticipacao.Rejeitada);
        var cancelada = await f.CriarParticipacao(viagem.Id, passageiroCancelado.Id, EstadoParticipacao.Cancelada);
        var controller = CriarController(f.Db, condutor.Id);

        Assert.IsType<ConflictResult>(await controller.Aceitar(aceite.Id, default));
        Assert.IsType<ConflictResult>(await controller.Rejeitar(aceite.Id, default));
        Assert.IsType<ConflictResult>(await controller.Aceitar(rejeitada.Id, default));
        Assert.IsType<ConflictResult>(await controller.Cancelar(rejeitada.Id, default));
        Assert.IsType<ConflictResult>(await controller.Cancelar(cancelada.Id, default));
    }

    [Fact]
    /// <summary>Verifica o cancelamento automático de pedidos pendentes ao concluir a viagem.</summary>
    public async Task ConcluirViagem_CancelaPedidosPendentes()
    {
        using var f = new Fixture();
        var condutor = await f.CriarUtilizador();
        var passageiro = await f.CriarUtilizador();
        var viagem = await f.CriarViagem(condutor.Id, partida: DateTime.UtcNow.AddMinutes(-1));
        var participacao = await f.CriarParticipacao(viagem.Id, passageiro.Id);
        var controller = new ViagensController(f.Db, new MapasService(new HttpClient(), new ConfigurationBuilder().Build()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, condutor.Id.ToString()) }, "Test"))
            }
        };

        var resultado = await controller.Concluir(viagem.Id, default);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(EstadoParticipacao.Cancelada, (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);
    }

    [Fact]
    public async Task CancelamentoAceite_NotificaRestantesPassageirosAceites() {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var passageiroQueSai = await f.CriarUtilizador();
        var passageiroQueFica = await f.CriarUtilizador();
        var passageiroPendente = await f.CriarUtilizador();

        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 3, lugaresDisponiveis: 1);

        var participacaoQueSai = await f.CriarParticipacao(viagem.Id, passageiroQueSai.Id, EstadoParticipacao.Aceite);

        await f.CriarParticipacao(viagem.Id, passageiroQueFica.Id, EstadoParticipacao.Aceite);

        await f.CriarParticipacao(viagem.Id, passageiroPendente.Id, EstadoParticipacao.Pendente);

        var resultado = await CriarController(f.Db, passageiroQueSai.Id).Cancelar(participacaoQueSai.Id, default);

        Assert.IsType<NoContentResult>(resultado);

        //Confirma que a participação foi cancelada e o lugar libertado.
        var participacaoAtualizada = await f.Db.Participacoes
            .AsNoTracking()
            .SingleAsync(p => p.Id == participacaoQueSai.Id);

        Assert.Equal(EstadoParticipacao.Cancelada, participacaoAtualizada.Estado);

        var viagemAtualizada = await f.Db.Viagens
            .AsNoTracking()
            .SingleAsync(v => v.Id == viagem.Id);

        Assert.Equal(2, viagemAtualizada.LugaresDisponiveis);

        //Apenas o passageiro aceite que permanece deve ser notificado.
        var notificacoes = await f.Db.Notificacoes
            .AsNoTracking()
            .ToListAsync();

        var notificacao = Assert.Single(notificacoes);

        Assert.Equal(passageiroQueFica.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.AlteracaoViagem, notificacao.Tipo);
        Assert.Contains("cancelou", notificacao.Conteudo);

        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == passageiroQueSai.Id);

        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == passageiroPendente.Id);
    }

    [Fact]
    public async Task AceitarPedido_NotificaRestantesPassageirosAceites() {
        using var f = new Fixture();

        var condutor = await f.CriarUtilizador();
        var novoPassageiro = await f.CriarUtilizador();
        var passageiroJaAceite = await f.CriarUtilizador();
        var passageiroPendente = await f.CriarUtilizador();

        var viagem = await f.CriarViagem(condutor.Id, lugaresOferecidos: 3, lugaresDisponiveis: 2);

        var pedido = await f.CriarParticipacao(viagem.Id, novoPassageiro.Id, EstadoParticipacao.Pendente);

        await f.CriarParticipacao(viagem.Id, passageiroJaAceite.Id, EstadoParticipacao.Aceite);

        await f.CriarParticipacao(viagem.Id, passageiroPendente.Id, EstadoParticipacao.Pendente);

        var controller = CriarController(f.Db, condutor.Id);

        var resultado = await controller.Aceitar(pedido.Id, default);

        Assert.IsType<NoContentResult>(resultado);

        //Confirma que o pedido foi aceite.
        var participacaoAtualizada = await f.Db.Participacoes
            .AsNoTracking()
            .SingleAsync(p => p.Id == pedido.Id);

        Assert.Equal(EstadoParticipacao.Aceite, participacaoAtualizada.Estado);

        //Apenas o passageiro que já estava aceite deve ser notificado.
        var notificacoes = await f.Db.Notificacoes
            .AsNoTracking()
            .ToListAsync();
        
        Assert.Equal(2, notificacoes.Count);

        var notificacaoPedidoAceite = Assert.Single(notificacoes.Where(n => n.Tipo == TipoNotificacao.PedidoAceite));

        Assert.Equal(novoPassageiro.Id, notificacaoPedidoAceite.UtilizadorId);

        var notificacaoAlteracaoViagem = Assert.Single(notificacoes.Where(n => n.Tipo == TipoNotificacao.AlteracaoViagem));

        Assert.Equal(passageiroJaAceite.Id, notificacaoAlteracaoViagem.UtilizadorId);

        Assert.DoesNotContain(notificacoes, n => n.UtilizadorId == passageiroPendente.Id);
    }



[Fact]
public async Task IT038_AceitarPedido_EnviaPushAoPassageiro()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);
    var participacao = await f.CriarParticipacao(
        viagem.Id, passageiro.Id);

    var push = new CampusCarpool.Tests.ServicoPushRegistador();

    var controller = CriarController(f.Db, condutor.Id, push);

    var resultado = await controller.Aceitar(
        participacao.Id, CancellationToken.None);

    Assert.IsType<NoContentResult>(resultado);

    var envio = Assert.Single(push.Envios);

    Assert.Equal(passageiro.Id, envio.UtilizadorId);
    Assert.Equal("PedidoAceite", envio.Tipo);

    Assert.Equal(
        EstadoParticipacao.Aceite,
        (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);

    var notificacao = Assert.Single(
        await f.Db.Notificacoes.ToListAsync());

    Assert.Equal(passageiro.Id, notificacao.UtilizadorId);
    Assert.Equal(TipoNotificacao.PedidoAceite, notificacao.Tipo);
}

[Fact]
public async Task IT038_RejeitarPedido_EnviaPushAoPassageiro()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);
    var participacao = await f.CriarParticipacao(
        viagem.Id, passageiro.Id);

    var push = new CampusCarpool.Tests.ServicoPushRegistador();

    var controller = CriarController(f.Db, condutor.Id, push);

    var resultado = await controller.Rejeitar(
        participacao.Id, CancellationToken.None);

    Assert.IsType<NoContentResult>(resultado);

    var envio = Assert.Single(push.Envios);

    Assert.Equal(passageiro.Id, envio.UtilizadorId);
    Assert.Equal("PedidoRejeitado", envio.Tipo);

    Assert.Equal(
        EstadoParticipacao.Rejeitada,
        (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);

    var notificacao = Assert.Single(
        await f.Db.Notificacoes.ToListAsync());

    Assert.Equal(passageiro.Id, notificacao.UtilizadorId);
    Assert.Equal(TipoNotificacao.PedidoRejeitado, notificacao.Tipo);
}

[Fact]
public async Task IT038_FalhaPush_NaoDeveImpedirAceitacao()
{
    using var f = new Fixture();

    var condutor = await f.CriarUtilizador();
    var passageiro = await f.CriarUtilizador();
    var viagem = await f.CriarViagem(condutor.Id);
    var participacao = await f.CriarParticipacao(
        viagem.Id, passageiro.Id);

    var push = new CampusCarpool.Tests.ServicoPushRegistador
    {
        SimularFalha = true
    };

    var controller = CriarController(f.Db, condutor.Id, push);

    var resultado = await controller.Aceitar(
        participacao.Id, CancellationToken.None);

    Assert.IsType<NoContentResult>(resultado);

    Assert.Equal(
        EstadoParticipacao.Aceite,
        (await f.Db.Participacoes.FindAsync(participacao.Id))!.Estado);

    Assert.Equal(
        1,
        (await f.Db.Viagens.FindAsync(viagem.Id))!.LugaresDisponiveis);

    var notificacao = Assert.Single(
        await f.Db.Notificacoes.ToListAsync());

    Assert.Equal(passageiro.Id, notificacao.UtilizadorId);
}

}
