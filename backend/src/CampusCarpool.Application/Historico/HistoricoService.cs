using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Historico;

/// <summary>Implementação do histórico pessoal (F009). Ver <see cref="IHistoricoService"/>.</summary>
/// <remarks>
/// As viagens aparecem em qualquer estado, incluindo as ativas futuras (RF-F009-02, RF-F009-03, D-029): o separador
/// Viagens junta as do condutor e aquelas em que o passageiro chegou a ser aceite; os pedidos nunca aceites ficam só
/// no separador Pedidos. As consultas correm uma a seguir à outra, porque partilham o contexto do pedido.
/// O repositório devolve entidades; as respostas montam-se aqui, com os estados em texto (BE-07).
/// </remarks>
public sealed class HistoricoService(IRepositorioHistorico historico) : IHistoricoService
{
    /// <inheritdoc />
    public async Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        IReadOnlyList<Viagem> comoCondutor = await historico.ListarViagensComoCondutorAsync(utilizadorId, ct);
        IReadOnlyList<Participacao> comoPassageiro = await historico.ListarParticipacoesComoPassageiroAsync(utilizadorId, ct);
        IReadOnlyList<Participacao> efetuados = await historico.ListarPedidosEfetuadosAsync(utilizadorId, ct);
        IReadOnlyList<Participacao> recebidos = await historico.ListarPedidosRecebidosAsync(utilizadorId, ct);
        IReadOnlyList<Notificacao> notificacoes = await historico.ListarNotificacoesAsync(utilizadorId, ct);
        IReadOnlyList<Aviso> avisos = await historico.ListarAvisosAsync(utilizadorId, ct);

        return new HistoricoResultado(
            comoCondutor.Select(ViagemComoCondutor).ToList(),
            comoPassageiro.Select(ViagemComoPassageiro).ToList(),
            efetuados.Select(Pedido).ToList(),
            recebidos.Select(Pedido).ToList(),
            notificacoes.Select(NotificacaoDoHistorico).ToList(),
            avisos.Select(AvisoDoHistorico).ToList());
    }

    /// <summary>Viagem do condutor: com os lugares oferecidos e sem participação.</summary>
    private static ViagemHistorico ViagemComoCondutor(Viagem viagem) => new(
        viagem.Id,
        viagem.Origem,
        viagem.Destino,
        viagem.PartidaEm,
        viagem.Estado.ToString(),
        viagem.LugaresOferecidos,
        null,
        null);

    /// <summary>Viagem do passageiro: o estado da viagem e o da sua participação, sem os lugares.</summary>
    private static ViagemHistorico ViagemComoPassageiro(Participacao participacao) => new(
        participacao.ViagemId,
        participacao.Viagem.Origem,
        participacao.Viagem.Destino,
        participacao.Viagem.PartidaEm,
        participacao.Viagem.Estado.ToString(),
        null,
        participacao.Id,
        participacao.Estado.ToString());

    /// <summary>Pedido efetuado ou recebido, com os dados da viagem pedida (RF-F009-04).</summary>
    private static PedidoHistorico Pedido(Participacao pedido) => new(
        pedido.Id,
        pedido.ViagemId,
        pedido.Viagem.Origem,
        pedido.Viagem.Destino,
        pedido.Viagem.PartidaEm,
        pedido.CriadoEm,
        pedido.Estado.ToString(),
        pedido.Recolha,
        pedido.Destino);

    private static NotificacaoHistorico NotificacaoDoHistorico(Notificacao notificacao) => new(
        notificacao.Id,
        notificacao.ViagemId,
        notificacao.Tipo.ToString(),
        notificacao.Titulo,
        notificacao.Conteudo,
        notificacao.CriadaEm,
        notificacao.LidaEm);

    private static AvisoHistorico AvisoDoHistorico(Aviso aviso) => new(aviso.Id, aviso.ViagemId, aviso.Conteudo, aviso.CriadoEm);
}
