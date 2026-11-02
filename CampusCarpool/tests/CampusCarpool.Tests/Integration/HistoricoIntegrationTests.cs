
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;


namespace CampusCarpool.Tests.Integration;

public sealed class HistoricoIntegrationTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection =
            new("Data Source=:memory:");

        public CampusCarpoolDbContext Db { get; }

        public Fixture()
        {
            connection.Open();

            var options =
                new DbContextOptionsBuilder<CampusCarpoolDbContext>()
                    .UseSqlite(connection)
                    .Options;

            Db = new CampusCarpoolDbContext(options);
            Db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Db.Dispose();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task HistoricoSemViagens_DevolveListasVazias()
    {
        using var fixture = new Fixture();

        IHistoricoService service =
            new HistoricoService(fixture.Db);

        var resultado = await service.ConsultarAsync(
            Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(resultado.ViagensComoCondutor);
        Assert.Empty(resultado.ViagensComoPassageiro);
    }

    [Fact]
    public async Task HistoricoCondutor_DevolveViagemPassada()
    {
        using var fixture = new Fixture();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt"
        };

        fixture.Db.Utilizadores.Add(utilizador);

        var viagem = new Viagem
        {
            CondutorId = utilizador.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(-2),
            Estado = "Concluída",
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3
        };

        fixture.Db.Viagens.Add(viagem);
        await fixture.Db.SaveChangesAsync();

        IHistoricoService service =
            new HistoricoService(fixture.Db);

        var resultado = await service.ConsultarAsync(
            utilizador.Id, CancellationToken.None);

        Assert.Single(resultado.ViagensComoCondutor);
        Assert.Empty(resultado.ViagensComoPassageiro);

        Assert.Equal(
            viagem.Id,
            resultado.ViagensComoCondutor[0].Id);
    }

    [Fact]
    public async Task HistoricoNaoMostraViagensDeOutros()
    {
        using var fixture = new Fixture();

        var utilizador = new Utilizador
        {
            Nome = "Outro Condutor",
            Email = "outro@estg.ipp.pt"
        };

        fixture.Db.Utilizadores.Add(utilizador);

        fixture.Db.Viagens.Add(new Viagem
        {
            CondutorId = utilizador.Id,
            Origem = "Felgueiras",
            Destino = "Porto",
            PartidaEm = DateTime.UtcNow.AddDays(-1),
            Estado = "Concluída",
            LugaresOferecidos = 2,
            LugaresDisponiveis = 2
        });

        await fixture.Db.SaveChangesAsync();

        IHistoricoService service =
            new HistoricoService(fixture.Db);

        var resultado = await service.ConsultarAsync(
            Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(resultado.ViagensComoCondutor);
        Assert.Empty(resultado.ViagensComoPassageiro);
    }
    
[Fact]
public async Task HistoricoPassageiro_DevolveViagemAceite()
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor",
        Email = "condutor2@estg.ipp.pt"
    };

    var passageiro = new Utilizador
    {
        Nome = "Passageiro",
        Email = "passageiro@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.AddRange(condutor, passageiro);

    var viagem = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Felgueiras",
        Destino = "Porto",
        PartidaEm = DateTime.UtcNow.AddDays(-2),
        Estado = "Concluída",
        LugaresOferecidos = 3,
        LugaresDisponiveis = 2
    };

    fixture.Db.Viagens.Add(viagem);

    fixture.Db.Participacoes.Add(new Participacao
    {
        ViagemId = viagem.Id,
        PassageiroId = passageiro.Id,
        Estado = EstadoParticipacao.Aceite
    });

    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var resultado = await service.ConsultarAsync(
        passageiro.Id, CancellationToken.None);

    Assert.Empty(resultado.ViagensComoCondutor);
    Assert.Single(resultado.ViagensComoPassageiro);
    Assert.Equal(
        viagem.Id,
        resultado.ViagensComoPassageiro[0].Id);
}

[Fact]
public async Task HistoricoCondutor_OrdenaPorDataDecrescente()
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor",
        Email = "ordenacao@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.Add(condutor);

    var antiga = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Porto",
        Destino = "Braga",
        PartidaEm = DateTime.UtcNow.AddDays(-10),
        Estado = "Concluída",
        LugaresOferecidos = 2,
        LugaresDisponiveis = 2
    };

    var recente = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Porto",
        Destino = "Felgueiras",
        PartidaEm = DateTime.UtcNow.AddDays(-1),
        Estado = "Concluída",
        LugaresOferecidos = 2,
        LugaresDisponiveis = 2
    };

    fixture.Db.Viagens.AddRange(antiga, recente);
    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var resultado = await service.ConsultarAsync(
        condutor.Id, CancellationToken.None);

    Assert.Equal(2, resultado.ViagensComoCondutor.Count);
    Assert.Equal(
        recente.Id,
        resultado.ViagensComoCondutor[0].Id);
    Assert.Equal(
        antiga.Id,
        resultado.ViagensComoCondutor[1].Id);
}


[Fact]
public async Task Historico_IncluiPedidosEfetuadosERecebidos()
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor",
        Email = "condutor.pedidos@estg.ipp.pt"
    };

    var passageiro = new Utilizador
    {
        Nome = "Passageiro",
        Email = "passageiro.pedidos@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.AddRange(condutor, passageiro);

    var viagem = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Felgueiras",
        Destino = "Porto",
        PartidaEm = DateTime.UtcNow.AddDays(2),
        Estado = "Ativa",
        LugaresOferecidos = 3,
        LugaresDisponiveis = 3
    };

    fixture.Db.Viagens.Add(viagem);

    var pedido = new Participacao
    {
        ViagemId = viagem.Id,
        PassageiroId = passageiro.Id,
        Estado = EstadoParticipacao.Pendente
    };

    fixture.Db.Participacoes.Add(pedido);
    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var historicoPassageiro = await service.ConsultarAsync(
        passageiro.Id, CancellationToken.None);

    var historicoCondutor = await service.ConsultarAsync(
        condutor.Id, CancellationToken.None);

    Assert.Single(historicoPassageiro.PedidosEfetuados);
    Assert.Equal(
        "Pendente",
        historicoPassageiro.PedidosEfetuados[0].Estado);

    Assert.Single(historicoCondutor.PedidosRecebidos);
    Assert.Equal(
        pedido.Id,
        historicoCondutor.PedidosRecebidos[0].Id);

    Assert.Empty(historicoCondutor.PedidosEfetuados);
    Assert.Empty(historicoPassageiro.PedidosRecebidos);
}

[Fact]
public async Task Historico_MostraApenasNotificacoesPessoais()
{
    using var fixture = new Fixture();

    // Criar dois utilizadores na base de dados de teste
    var utilizador = new Utilizador
    {
        Nome = "Utilizador Teste",
        Email = "notificacoes1@estg.ipp.pt"
    };

    var outroUtilizador = new Utilizador
    {
        Nome = "Outro Utilizador",
        Email = "notificacoes2@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.AddRange(
        utilizador, outroUtilizador);

    await fixture.Db.SaveChangesAsync();

    // Criar uma notificacao para cada utilizador
    fixture.Db.Notificacoes.AddRange(
        new Notificacao
        {
            UtilizadorId = utilizador.Id,
            Titulo = "Notificacao pessoal",
            Conteudo = "Mensagem pessoal"
        },
        new Notificacao
        {
            UtilizadorId = outroUtilizador.Id,
            Titulo = "Notificacao privada de outro",
            Conteudo = "Nao deve aparecer"
        });

    await fixture.Db.SaveChangesAsync();

    // Consultar apenas o historico do primeiro utilizador
    var service = new HistoricoService(fixture.Db);

    var resultado = await service.ConsultarAsync(
        utilizador.Id, CancellationToken.None);

    // Verificar que apenas a notificacao pessoal aparece
    Assert.Single(resultado.Notificacoes);

    Assert.Equal(
        "Notificacao pessoal",
        resultado.Notificacoes[0].Titulo);
}

[Fact]
public async Task Historico_AvisosRespeitamParticipacaoAceite()
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor",
        Email = "condutor.avisos@estg.ipp.pt"
    };

    var aceite = new Utilizador
    {
        Nome = "Aceite",
        Email = "aceite@estg.ipp.pt"
    };

    var rejeitado = new Utilizador
    {
        Nome = "Rejeitado",
        Email = "rejeitado@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.AddRange(
        condutor, aceite, rejeitado);

    var viagem = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Porto",
        Destino = "Braga",
        PartidaEm = DateTime.UtcNow.AddDays(1),
        Estado = "Ativa",
        LugaresOferecidos = 3,
        LugaresDisponiveis = 2
    };

    fixture.Db.Viagens.Add(viagem);

    fixture.Db.Participacoes.AddRange(
        new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = aceite.Id,
            Estado = EstadoParticipacao.Aceite
        },
        new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = rejeitado.Id,
            Estado = EstadoParticipacao.Rejeitada
        });

    fixture.Db.Avisos.Add(new Aviso
    {
        ViagemId = viagem.Id,
        CondutorId = condutor.Id,
        Conteudo = "A partida sera no local combinado"
    });

    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var historicoCondutor = await service.ConsultarAsync(
        condutor.Id, CancellationToken.None);

    var historicoAceite = await service.ConsultarAsync(
        aceite.Id, CancellationToken.None);

    var historicoRejeitado = await service.ConsultarAsync(
        rejeitado.Id, CancellationToken.None);

    Assert.Single(historicoCondutor.Avisos);
    Assert.Single(historicoAceite.Avisos);
    Assert.Empty(historicoRejeitado.Avisos);
}


[Theory]
[InlineData(EstadoParticipacao.Pendente)]
[InlineData(EstadoParticipacao.Rejeitada)]
[InlineData(EstadoParticipacao.Cancelada)]
public async Task HistoricoPassageiro_NaoIncluiParticipacoesNaoAceites(
    EstadoParticipacao estado)
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor",
        Email = "condutor.estados@estg.ipp.pt"
    };

    var passageiro = new Utilizador
    {
        Nome = "Passageiro",
        Email = "passageiro.estados@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.AddRange(condutor, passageiro);

    var viagem = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Felgueiras",
        Destino = "Porto",
        PartidaEm = DateTime.UtcNow.AddDays(-2),
        Estado = "Concluída",
        LugaresOferecidos = 3,
        LugaresDisponiveis = 3
    };

    fixture.Db.Viagens.Add(viagem);

    fixture.Db.Participacoes.Add(new Participacao
    {
        ViagemId = viagem.Id,
        PassageiroId = passageiro.Id,
        Estado = estado
    });

    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var resultado = await service.ConsultarAsync(
        passageiro.Id, CancellationToken.None);

    Assert.Empty(resultado.ViagensComoPassageiro);

    // O pedido continua no historico, com o respetivo estado.
    Assert.Single(resultado.PedidosEfetuados);
    Assert.Equal(
        estado.ToString(),
        resultado.PedidosEfetuados[0].Estado);
}

[Fact]
public async Task HistoricoCondutor_MantemViagemIndisponibilizada()
{
    using var fixture = new Fixture();

    var condutor = new Utilizador
    {
        Nome = "Condutor Historico",
        Email = "condutor.indisponivel@estg.ipp.pt"
    };

    fixture.Db.Utilizadores.Add(condutor);

    var viagem = new Viagem
    {
        CondutorId = condutor.Id,
        Origem = "Felgueiras",
        Destino = "Porto",
        PartidaEm = DateTime.UtcNow.AddDays(2),
        Estado = "Indisponibilizada",
        LugaresOferecidos = 3,
        LugaresDisponiveis = 3
    };

    fixture.Db.Viagens.Add(viagem);
    await fixture.Db.SaveChangesAsync();

    var service = new HistoricoService(fixture.Db);

    var resultado = await service.ConsultarAsync(
        condutor.Id, CancellationToken.None);

    Assert.Single(resultado.ViagensComoCondutor);
    Assert.Equal(
        viagem.Id,
        resultado.ViagensComoCondutor[0].Id);

    Assert.Equal(
        "Indisponibilizada",
        resultado.ViagensComoCondutor[0].Estado);
}


}
