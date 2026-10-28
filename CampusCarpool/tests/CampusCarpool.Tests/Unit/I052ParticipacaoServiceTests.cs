using CampusCarpool.Application.Interfaces;
using CampusCarpool.Application.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class I052ParticipacaoServiceTests
{
    private readonly ParticipacoesFake participacoes = new();
    private readonly ViagensFake viagens = new();
    private readonly TrabalhoFake trabalho = new();
    private readonly NotificacoesFake notificacoes = new();
    private ParticipacaoService CriarServico() => new(participacoes, viagens, trabalho, notificacoes);

    private (Participacao pedido, Viagem viagem, Guid condutor) Preparar()
    {
        var condutor = Guid.NewGuid();
        var viagem = new Viagem { CondutorId = condutor, PartidaEm = DateTime.UtcNow.AddDays(2), LugaresOferecidos = 2, LugaresDisponiveis = 2, Estado = "Ativa" };
        var pedido = new Participacao { ViagemId = viagem.Id, PassageiroId = Guid.NewGuid() };
        viagens.Item = viagem;
        participacoes.Item = pedido;
        return (pedido, viagem, condutor);
    }

    [Fact] public async Task Aceitar_PedidoInexistente_NaoEncontrada()
    {
        var resultado = await CriarServico().AceitarAsync(Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal(ResultadoGestaoParticipacao.NaoEncontrada, resultado);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact] public async Task Aceitar_OutroUtilizador_NaoAutorizado()
    {
        var (pedido, _, _) = Preparar();
        var resultado = await CriarServico().AceitarAsync(pedido.Id, Guid.NewGuid(), default);
        Assert.Equal(ResultadoGestaoParticipacao.NaoAutorizado, resultado);
        Assert.Empty(notificacoes.Enviadas);
    }

    [Fact] public async Task Aceitar_SemLugares_NaoAlteraEstado()
    {
        var (pedido, viagem, condutor) = Preparar();
        viagem.LugaresOferecidos = 0;
        viagem.LugaresDisponiveis = 0;
        var resultado = await CriarServico().AceitarAsync(pedido.Id, condutor, default);
        Assert.Equal(ResultadoGestaoParticipacao.SemCapacidade, resultado);
        Assert.Equal(EstadoParticipacao.Pendente, pedido.Estado);
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact] public async Task Aceitar_CapacidadeInconsistente_NaoConfirma()
    {
        var (pedido, viagem, condutor) = Preparar();
        viagem.LugaresDisponiveis = 1;
        Assert.Equal(ResultadoGestaoParticipacao.DadosInconsistentes, await CriarServico().AceitarAsync(pedido.Id, condutor, default));
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact] public async Task Aceitar_PedidoValido_DescontaLugarENotificaPassageiro()
    {
        var (pedido, viagem, condutor) = Preparar();
        var resultado = await CriarServico().AceitarAsync(pedido.Id, condutor, default);
        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, resultado);
        Assert.Equal(EstadoParticipacao.Aceite, pedido.Estado);
        Assert.Equal(1, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
        Assert.Contains(notificacoes.Enviadas, n => n.id == pedido.PassageiroId && n.tipo == TipoNotificacao.PedidoAceite);
    }

    [Fact] public async Task Rejeitar_PedidoValido_NotificaSemOcuparLugar()
    {
        var (pedido, viagem, condutor) = Preparar();
        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, await CriarServico().RejeitarAsync(pedido.Id, condutor, default));
        Assert.Equal(EstadoParticipacao.Rejeitada, pedido.Estado);
        Assert.Equal(2, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
        Assert.Contains(notificacoes.Enviadas, n => n.tipo == TipoNotificacao.PedidoRejeitado && n.id == pedido.PassageiroId);
    }

    [Fact] public async Task Rejeitar_PedidoJaAceite_Conflito()
    {
        var (pedido, _, condutor) = Preparar();
        pedido.Estado = EstadoParticipacao.Aceite;
        Assert.Equal(ResultadoGestaoParticipacao.Conflito, await CriarServico().RejeitarAsync(pedido.Id, condutor, default));
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    [Fact] public async Task Cancelar_PendentePeloPassageiro_NaoAlteraCapacidade()
    {
        var (pedido, viagem, _) = Preparar();
        Assert.Equal(ResultadoGestaoParticipacao.Sucesso, await CriarServico().CancelarAsync(pedido.Id, pedido.PassageiroId, default));
        Assert.Equal(EstadoParticipacao.Cancelada, pedido.Estado);
        Assert.Equal(2, viagem.LugaresDisponiveis);
        Assert.Equal(1, trabalho.Confirmacoes);
    }

    [Fact] public async Task Cancelar_PorTerceiro_NaoAutorizado()
    {
        var (pedido, _, _) = Preparar();
        Assert.Equal(ResultadoGestaoParticipacao.NaoAutorizado, await CriarServico().CancelarAsync(pedido.Id, Guid.NewGuid(), default));
        Assert.Equal(0, trabalho.Confirmacoes);
    }

    private sealed class ParticipacoesFake : IRepositorioParticipacoes
    {
        public Participacao? Item { get; set; }
        public Task<Participacao?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
        public Task<IReadOnlyList<Participacao>> ListarPorViagemAsync(Guid id, EstadoParticipacao? estado, CancellationToken ct) => Task.FromResult<IReadOnlyList<Participacao>>(Item is not null && Item.ViagemId == id && (estado is null || Item.Estado == estado) ? new[] { Item } : Array.Empty<Participacao>());
        public Task<IReadOnlyList<Participacao>> ListarPorPassageiroAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<Participacao>>(Array.Empty<Participacao>());
        public Task<bool> ExisteAtivaAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) => Task.FromResult(false);
    }
    private sealed class ViagensFake : IRepositorioViagens
    {
        public Viagem? Item { get; set; }
        public Task<Viagem?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
    }
    private sealed class TrabalhoFake : IUnidadeDeTrabalho
    {
        public int Confirmacoes { get; private set; }
        public void Confirmar() => Confirmacoes++;
    }
    private sealed class NotificacoesFake : INotificacaoService
    {
        public List<(Guid id, TipoNotificacao tipo)> Enviadas { get; } = new();
        public Task<Notificacao> CriarAsync(Guid id, TipoNotificacao tipo, string mensagem, CancellationToken ct)
        {
            Enviadas.Add((id, tipo));
            return Task.FromResult(new Notificacao { UtilizadorId = id, Tipo = tipo, Conteudo = mensagem });
        }
        public Task<PaginaNotificacoes> ListarAsync(Guid id, int pagina, int tamanho, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> MarcarComoLidaAsync(Guid id, Guid utilizador, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> MarcarTodasComoLidasAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
