using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Regras do <c>ParticipacaoService</c> (aceitar, rejeitar, cancelar) com repositórios, unidade de trabalho e notificações falsos (I052).</summary>
/// <remarks>
/// Teste unitário: verifica também que nada é gravado nem notificado quando a operação é recusada.
/// </remarks>
public sealed class I052ParticipacaoServiceTests
{
    private readonly ParticipacoesFake participacoes = new();
    private readonly ViagensFake viagens = new();
    private readonly TrabalhoFake trabalho = new();
    private readonly NotificacoesFake notificacoes = new();
    private ParticipacaoService CriarServico() =>
    new(participacoes, viagens, trabalho, notificacoes,
        new CampusCarpool.Tests.NotificadorPedidosFalso(),
        new CampusCarpool.Tests.RotaViagemFalsa(),
        null!,
        null!);

    private (Participacao pedido, Viagem viagem, Guid condutor) Preparar()
    {
        var condutor = Guid.NewGuid();
        var viagem = new Viagem { DestinoLatitude = 41.55m, CondutorId = condutor, PartidaEm = DateTime.UtcNow.AddDays(2), LugaresOferecidos = 2, LugaresDisponiveis = 2, Estado = EstadoViagem.Ativa };
        var pedido = new Participacao { ViagemId = viagem.Id, PassageiroId = Guid.NewGuid() };
        viagens.Item = viagem;
        participacoes.Item = pedido;
        return (pedido, viagem, condutor);
    }

    [Fact]
    public async Task Aceitar_PedidoInexistente_NaoEncontrada()
    {
        var resultado = await CriarServico().AceitarAsync(Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro?.Tipo);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact]
    public async Task Aceitar_OutroUtilizador_NaoAutorizado()
    {
        var (pedido, _, _) = Preparar();
        var resultado = await CriarServico().AceitarAsync(pedido.Id, Guid.NewGuid(), default);
        Assert.Equal(TipoErro.Proibido, resultado.Erro?.Tipo);
        Assert.Empty(notificacoes.Enviadas);
    }

    [Fact]
    public async Task Aceitar_SemLugares_NaoAlteraEstado()
    {
        var (pedido, viagem, condutor) = Preparar();
        viagem.LugaresOferecidos = 0;
        viagem.LugaresDisponiveis = 0;
        var resultado = await CriarServico().AceitarAsync(pedido.Id, condutor, default);
        Assert.Equal("Sem lugares disponíveis.", resultado.Erro?.Mensagem);
        Assert.Equal(EstadoParticipacao.Pendente, pedido.Estado);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact]
    public async Task Aceitar_CapacidadeInconsistente_NaoConfirma()
    {
        var (pedido, viagem, condutor) = Preparar();
        viagem.LugaresDisponiveis = 1;
        Assert.Equal("A capacidade da viagem está inconsistente.", (await CriarServico().AceitarAsync(pedido.Id, condutor, default)).Erro?.Mensagem);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact]
    public async Task Aceitar_PedidoValido_DescontaLugarENotificaPassageiro()
    {
        var (pedido, viagem, condutor) = Preparar();
        var resultado = await CriarServico().AceitarAsync(pedido.Id, condutor, default);
        Assert.Null(resultado.Erro);
        Assert.NotNull(pedido.AceiteEm); // I110: guardado para o histórico depois de cancelamentos (RF-F009-08)
        Assert.Equal(EstadoParticipacao.Aceite, pedido.Estado);
        Assert.Equal(1, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
        Assert.Contains(notificacoes.Enviadas, n => n.id == pedido.PassageiroId && n.tipo == TipoNotificacao.PedidoAceite);
    }

    [Fact]
    public async Task Rejeitar_PedidoValido_NotificaSemOcuparLugar()
    {
        var (pedido, viagem, condutor) = Preparar();
        Assert.Null((await CriarServico().RejeitarAsync(pedido.Id, condutor, default)).Erro);
        Assert.Equal(EstadoParticipacao.Rejeitada, pedido.Estado);
        Assert.Equal(2, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
        Assert.Contains(notificacoes.Enviadas, n => n.tipo == TipoNotificacao.PedidoRejeitado && n.id == pedido.PassageiroId);
    }

    [Fact]
    public async Task Rejeitar_PedidoJaAceite_Conflito()
    {
        var (pedido, _, condutor) = Preparar();
        pedido.Estado = EstadoParticipacao.Aceite;
        Assert.Equal(TipoErro.Conflito, (await CriarServico().RejeitarAsync(pedido.Id, condutor, default)).Erro?.Tipo);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact]
    public async Task Cancelar_PendentePeloPassageiro_NaoAlteraCapacidade()
    {
        var (pedido, viagem, _) = Preparar();
        Assert.Null((await CriarServico().CancelarAsync(pedido.Id, pedido.PassageiroId, default)).Erro);
        Assert.Equal(EstadoParticipacao.Cancelada, pedido.Estado);
        Assert.Equal(2, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
    }

    [Fact]
    public async Task Cancelar_PorTerceiro_NaoAutorizado()
    {
        var (pedido, _, _) = Preparar();
        Assert.Equal(TipoErro.Proibido, (await CriarServico().CancelarAsync(pedido.Id, Guid.NewGuid(), default)).Erro?.Tipo);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    private sealed class ParticipacoesFake : IRepositorioParticipacoes
    {
        public Participacao? Item { get; set; }
        public Task<Participacao?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
        public Task<IReadOnlyList<Participacao>> ListarPorViagemAsync(Guid id, EstadoParticipacao? estado, CancellationToken ct) => Task.FromResult<IReadOnlyList<Participacao>>(Item is not null && Item.ViagemId == id && (estado is null || Item.Estado == estado) ? new[] { Item } : Array.Empty<Participacao>());
        public Task<IReadOnlyList<Participacao>> ListarPorPassageiroAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<Participacao>>(Array.Empty<Participacao>());
        public Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExistePedidoAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExisteAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) => Task.FromResult(false);
        public Task<IReadOnlyList<Participacao>> ListarPendentesParaAlterarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<Participacao>>(Array.Empty<Participacao>());
        public Task<bool> AdicionarPedidoAsync(Participacao pedido, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Participacao>> ListarAtivasParaAlterarAsync(Guid viagemId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<PassageiroAceite>> ListarPassageirosAceitesAsync(Guid viagemId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<PedidoPendente>> ListarPedidosPendentesAsync(Guid viagemId, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class ViagensFake : IRepositorioViagens
    {
        public Viagem? Item { get; set; }
        public Task<Viagem?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
        public Task<IReadOnlyList<ViagemEmCurso>> ListarAtivasJaPartidasAsync(DateTime agora, CancellationToken ct) => Task.FromResult<IReadOnlyList<ViagemEmCurso>>(Array.Empty<ViagemEmCurso>());
        public Task<PaginaViagens> PesquisarAsync(FiltroPesquisaViagens filtro, DateTime agora, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<ViagemDoCondutor>> ListarAtivasDoCondutorAsync(Guid condutorId, CancellationToken ct) => throw new NotSupportedException();
        public void Adicionar(Viagem viagem) => throw new NotSupportedException();
    }
    private sealed class TrabalhoFake : IUnidadeDeTrabalho
    {
        public int Confirmacoes { get; private set; }
        public Task ConfirmarAsync(CancellationToken ct)
        {
            Confirmacoes++;
            return Task.CompletedTask;
        }
        public Task<bool> TentarConfirmarAsync(CancellationToken ct) => Task.FromResult(true);
        public void DescartarAlteracoes()
        {
        }
    }
    private sealed class NotificacoesFake : INotificacaoService
    {
        public List<(Guid id, TipoNotificacao tipo)> Enviadas { get; } = new();
        public Notificacao Registar(Guid id, TipoNotificacao tipo, string mensagem)
        {
            Enviadas.Add((id, tipo));
            return new Notificacao { UtilizadorId = id, Tipo = tipo, Conteudo = mensagem };
        }
        public void RegistarAlteracaoViagem(Guid condutorId, IEnumerable<Guid> passageiros, Guid origemId, string mensagem)
        {
            foreach (var id in passageiros.Append(condutorId).Where(id => id != origemId).Distinct())
                Registar(id, TipoNotificacao.AlteracaoViagem, mensagem);
        }
        public void RegistarAviso(Guid viagemId, IEnumerable<Guid> passageiros, string texto) => throw new NotSupportedException();
        public Task EnviarPushAsync(Notificacao notificacao, CancellationToken ct) => Task.CompletedTask;
        public Task<PaginaNotificacoes> ListarAsync(Guid id, int pagina, int tamanho, CancellationToken ct) => throw new NotSupportedException();
        public Task<Resultado> MarcarComoLidaAsync(Guid id, Guid utilizador, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> MarcarTodasComoLidasAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
