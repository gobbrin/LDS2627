using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// DC-37: a operação e as suas notificações são uma só gravação, e o tempo real e o push só saem depois dela
/// (RF-F006-01, RF-F006-02, RF-F006-06, RF-F006-14).
/// </summary>
/// <remarks>Antes, a decisão ficava gravada mesmo que a notificação falhasse: a API dava 500 e a repetição dava 409.</remarks>
public sealed class NotificacoesMesmaGravacaoTests : IAsyncLifetime, IDisposable
{
    private sealed class ContadorDeGravacoes : SaveChangesInterceptor
    {
        public int Gravacoes { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Gravacoes++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>Grava a sério e cancela o pedido a seguir, como um cliente que fecha a aplicação nesse instante.</summary>
    private sealed class UnidadeQueCancelaDepoisDeGravar(IUnidadeDeTrabalho real, CancellationTokenSource pedido) : IUnidadeDeTrabalho
    {
        public async Task ConfirmarAsync(CancellationToken ct)
        {
            await real.ConfirmarAsync(ct);
            await pedido.CancelAsync();
        }

        public Task<Exception?> TentarConfirmarAsync(CancellationToken ct) => real.TentarConfirmarAsync(ct);

        public void DescartarAlteracoes() => real.DescartarAlteracoes();
    }

    private sealed class TempoRealRegistador : Application.Abstractions.Integrations.INotificadorPedidosTempoReal
    {
        public int Eventos { get; private set; }

        private Task Registar()
        {
            Eventos++;
            return Task.CompletedTask;
        }

        public Task PedidoRecebidoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Registar();
        public Task PedidoAceiteAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Registar();
        public Task PedidoRejeitadoAsync(Guid destinatarioId, Guid pedidoId, CancellationToken ct) => Registar();
    }

    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly ContadorDeGravacoes contador = new();
    private readonly FilaNotificacoesPushFalsa fila = new();
    private readonly TempoRealRegistador tempoReal = new();
    private CampusCarpoolDbContext db = null!;
    private Utilizador condutor = null!;
    private Utilizador passageiro = null!;
    private Viagem viagem = null!;
    private Participacao pedido = null!;

    private DbContextOptions<CampusCarpoolDbContext> Opcoes(bool contar) =>
        (contar
            ? new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao).AddInterceptors(contador)
            : new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao)).Options;

    public async Task InitializeAsync()
    {
        await ligacao.OpenAsync();
        db = new CampusCarpoolDbContext(Opcoes(contar: true));
        await db.Database.CriarEsquemaDeTesteAsync();

        condutor = new Utilizador { Nome = "Condutor", Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };
        passageiro = new Utilizador { Nome = "Passageiro", Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };
        viagem = new Viagem
        {
            CondutorId = condutor.Id,
            Origem = "Felgueiras",
            Destino = "Porto",
            OrigemLatitude = 41.3m,
            OrigemLongitude = -8.2m,
            DestinoLatitude = 41.1m,
            DestinoLongitude = -8.6m,
            PartidaEm = DateTime.UtcNow.AddDays(1),
            LugaresOferecidos = 2,
            LugaresDisponiveis = 2
        };
        pedido = new Participacao { ViagemId = viagem.Id, PassageiroId = passageiro.Id };

        db.AddRange(condutor, passageiro, viagem, pedido);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    private ParticipacaoService CriarServico(IUnidadeDeTrabalho? trabalho = null)
    {
        var unidade = trabalho ?? new UnidadeDeTrabalhoEfCore(db);

        return new ParticipacaoService(
            new RepositorioParticipacoes(db),
            new RepositorioViagens(db),
            unidade,
            new NotificacaoService(new RepositorioNotificacoes(db), fila, unidade),
            tempoReal,
            new RotaViagemFalsa(),
            null!,
            new RepositorioUtilizadores(db));
    }

    private async Task<(EstadoParticipacao Estado, int Notificacoes)> LerDaBdAsync()
    {
        await using var verificacao = new CampusCarpoolDbContext(Opcoes(contar: false));

        var estado = await verificacao.Participacoes.AsNoTracking()
            .Where(p => p.Id == pedido.Id)
            .Select(p => p.Estado)
            .SingleAsync();

        return (estado, await verificacao.Notificacoes.CountAsync());
    }

    [Fact]
    public async Task I110_67_Aceitar_GravaDecisaoENotificacoesDeUmaVez()
    {
        var gravacoesAntes = contador.Gravacoes;

        var resultado = await CriarServico().AceitarAsync(pedido.Id, condutor.Id, default);

        Assert.Null(resultado.Erro);
        Assert.Equal(gravacoesAntes + 1, contador.Gravacoes);
        Assert.Equal((EstadoParticipacao.Aceite, 1), await LerDaBdAsync());
        Assert.Single(fila.Notificacoes);
        Assert.Equal(1, tempoReal.Eventos);
    }

    [Fact]
    public async Task I110_68_Rejeitar_GravaDecisaoENotificacaoDeUmaVez()
    {
        var gravacoesAntes = contador.Gravacoes;

        var resultado = await CriarServico().RejeitarAsync(pedido.Id, condutor.Id, default);

        Assert.Null(resultado.Erro);
        Assert.Equal(gravacoesAntes + 1, contador.Gravacoes);
        Assert.Equal((EstadoParticipacao.Rejeitada, 1), await LerDaBdAsync());
    }

    [Fact]
    public async Task I110_69_FalhaAoGravar_NaoFicaNadaNemHaAnuncios()
    {
        var falha = new UnidadeDeTrabalhoFalsa { Falha = new RegraDeNegocioException("Conflito") };

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => CriarServico(falha).AceitarAsync(pedido.Id, condutor.Id, default));

        Assert.Equal((EstadoParticipacao.Pendente, 0), await LerDaBdAsync());
        Assert.Empty(fila.Notificacoes);
        Assert.Equal(0, tempoReal.Eventos);
    }

    [Fact]
    public async Task I110_70_ClienteSaiDepoisDeGravar_PushETempoRealSaemNaMesma()
    {
        using var pedidoHttp = new CancellationTokenSource();
        var trabalho = new UnidadeQueCancelaDepoisDeGravar(new UnidadeDeTrabalhoEfCore(db), pedidoHttp);

        var resultado = await CriarServico(trabalho).AceitarAsync(pedido.Id, condutor.Id, pedidoHttp.Token);

        Assert.Null(resultado.Erro);
        Assert.Single(fila.Notificacoes);
        Assert.Equal(1, tempoReal.Eventos);
    }

    [Fact]
    public async Task I110_71_Mensagem_GravaMensagemENotificacaoDeUmaVez()
    {
        var gravacoesAntes = contador.Gravacoes;
        var unidade = new UnidadeDeTrabalhoEfCore(db);
        var servico = new MensagemService(
            new RepositorioMensagens(db),
            new RepositorioUtilizadores(db),
            new NotificacaoService(new RepositorioNotificacoes(db), fila, unidade),
            unidade);

        var resultado = await servico.EnviarAsync(passageiro.Id, condutor.Id, "Até amanhã.", default);

        Assert.Null(resultado.Erro);
        Assert.Equal(gravacoesAntes + 1, contador.Gravacoes);
        Assert.Equal(1, (await LerDaBdAsync()).Notificacoes);
    }

    [Fact]
    public async Task I110_72_MarcarComoLida_DeOutroUtilizador_NaoEncontrada()
    {
        var notificacao = new Notificacao { UtilizadorId = condutor.Id, Tipo = TipoNotificacao.Aviso, Conteudo = "Texto" };
        db.Notificacoes.Add(notificacao);
        await db.SaveChangesAsync();

        var servico = new NotificacaoService(new RepositorioNotificacoes(db), fila, new UnidadeDeTrabalhoEfCore(db));

        var resultado = await servico.MarcarComoLidaAsync(notificacao.Id, passageiro.Id, default);

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro?.Tipo);
    }
}
