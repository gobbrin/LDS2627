using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Migrations;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — DEF-BE-008 com RF-F009-08: a indisponibilização cancela as participações
// aceites (P-25), mas a viagem continua no histórico do passageiro, com o estado atual.
// SQLite em memória; o controller é instanciado diretamente, sem notificações.
public sealed class IndisponibilizacaoHistoricoTests : IDisposable
{
    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;

    public IndisponibilizacaoHistoricoTests()
    {
        _ligacao.Open();
        _db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(_ligacao).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _ligacao.Dispose();
    }

    private async Task<Utilizador> CriarUtilizadorAsync()
    {
        var utilizador = new Utilizador { Nome = "Teste", Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };
        _db.Utilizadores.Add(utilizador);
        await _db.SaveChangesAsync();
        return utilizador;
    }

    private async Task<Viagem> CriarViagemAsync(Guid condutorId, DateTime partidaEm, int aceites)
    {
        var viagem = new Viagem
        {
            CondutorId = condutorId,
            Origem = "ESTG",
            Destino = "Porto",
            PartidaEm = partidaEm,
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3 - aceites
        };
        _db.Viagens.Add(viagem);
        await _db.SaveChangesAsync();
        return viagem;
    }

    private async Task<Participacao> CriarParticipacaoAsync(Guid viagemId, Guid passageiroId, EstadoParticipacao estado, DateTime? aceiteEm)
    {
        var participacao = new Participacao { ViagemId = viagemId, PassageiroId = passageiroId, Estado = estado, AceiteEm = aceiteEm };
        _db.Participacoes.Add(participacao);
        await _db.SaveChangesAsync();
        return participacao;
    }

    private static readonly IServicoMapasRotas MapasSemConfiguracao = new MapasFalsos().CriarServico(configurado: false);

    private ViagensController CriarController(Guid utilizadorId) =>
        new(_db, MapasSemConfiguracao, ServicosDeTeste.CriarRotas(_db, MapasSemConfiguracao), ServicosDeTeste.CriarConsultas(_db))
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

    [Fact]
    public async Task I110_08_Indisponibilizar_ViagemContinuaNoHistoricoDoPassageiroAceite()
    {
        var condutor = await CriarUtilizadorAsync();
        var aceite = await CriarUtilizadorAsync();
        var pendente = await CriarUtilizadorAsync();
        var viagem = await CriarViagemAsync(condutor.Id, DateTime.UtcNow.AddDays(2), aceites: 1);
        await CriarParticipacaoAsync(viagem.Id, aceite.Id, EstadoParticipacao.Aceite, DateTime.UtcNow.AddHours(-1));
        await CriarParticipacaoAsync(viagem.Id, pendente.Id, EstadoParticipacao.Pendente, null);

        Assert.IsType<NoContentResult>(await CriarController(condutor.Id).Indisponibilizar(viagem.Id, default));

        var historico = ServicosDeTeste.CriarHistorico(_db);
        var doAceite = await historico.ConsultarAsync(aceite.Id, default);
        var registo = Assert.Single(doAceite.ViagensComoPassageiro);
        Assert.Equal(viagem.Id, registo.Id);
        Assert.Equal(nameof(EstadoViagem.Indisponibilizada), registo.Estado);
        Assert.Equal(nameof(EstadoParticipacao.Cancelada), registo.EstadoParticipacao);

        // O pedido que nunca foi aceite não é uma participação: fica só nos pedidos efetuados.
        var doPendente = await historico.ConsultarAsync(pendente.Id, default);
        Assert.Empty(doPendente.ViagensComoPassageiro);
        Assert.Equal(nameof(EstadoParticipacao.Cancelada), Assert.Single(doPendente.PedidosEfetuados).Estado);
    }

    [Fact]
    /// <summary>RF-F009-03 / D-029: a participação cancelada depois de aceite fica no separador Viagens, com o estado.</summary>
    public async Task I110_09_ParticipacaoCanceladaPeloPassageiro_ContinuaNoHistorico()
    {
        var condutor = await CriarUtilizadorAsync();
        var passageiro = await CriarUtilizadorAsync();
        var viagem = await CriarViagemAsync(condutor.Id, DateTime.UtcNow.AddDays(-1), aceites: 0);
        await CriarParticipacaoAsync(viagem.Id, passageiro.Id, EstadoParticipacao.Cancelada, DateTime.UtcNow.AddDays(-2));

        var historico = await ServicosDeTeste.CriarHistorico(_db).ConsultarAsync(passageiro.Id, default);

        var registo = Assert.Single(historico.ViagensComoPassageiro);
        Assert.Equal(nameof(EstadoParticipacao.Cancelada), registo.EstadoParticipacao);
    }

    [Fact]
    /// <summary>RF-F009-02, RF-F009-03 / D-029 (A5): o histórico inclui as viagens ativas ainda por realizar.</summary>
    public async Task I110_43_ViagemAtivaFutura_ApareceNoHistorico()
    {
        var condutor = await CriarUtilizadorAsync();
        var passageiro = await CriarUtilizadorAsync();
        var viagem = await CriarViagemAsync(condutor.Id, DateTime.UtcNow.AddDays(2), aceites: 1);
        await CriarParticipacaoAsync(viagem.Id, passageiro.Id, EstadoParticipacao.Aceite, DateTime.UtcNow.AddHours(-1));

        var servico = ServicosDeTeste.CriarHistorico(_db);

        Assert.Equal(viagem.Id, Assert.Single((await servico.ConsultarAsync(condutor.Id, default)).ViagensComoCondutor).Id);
        Assert.Equal(viagem.Id, Assert.Single((await servico.ConsultarAsync(passageiro.Id, default)).ViagensComoPassageiro).Id);
    }

    [Fact]
    public void I110_10_Migration_PreencheAceiteEmDasParticipacoesAceites()
    {
        var operacoes = new I110_ParticipacaoAceiteEm().UpOperations;

        Assert.Collection(
            operacoes,
            primeira => Assert.Equal("AceiteEm", Assert.IsType<AddColumnOperation>(primeira).Name),
            segunda =>
            {
                var sql = Assert.IsType<SqlOperation>(segunda).Sql;
                Assert.StartsWith("EXEC(", sql);
                Assert.Contains("SET [AceiteEm] = [CriadoEm]", sql);
                Assert.Contains("[Estado] = N''Aceite''", sql);
            });
    }
}
