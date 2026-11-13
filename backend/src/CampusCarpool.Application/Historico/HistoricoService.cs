using CampusCarpool.Application.Abstractions.Persistence;

namespace CampusCarpool.Application.Historico;

/// <summary>Implementação do histórico pessoal (F009). Ver <see cref="IHistoricoService"/>.</summary>
/// <remarks>
/// As viagens aparecem em qualquer estado, incluindo as ativas futuras (RF-F009-02, RF-F009-03, D-029): o separador
/// Viagens junta as do condutor e aquelas em que o passageiro chegou a ser aceite; os pedidos nunca aceites ficam só
/// no separador Pedidos. As consultas correm uma a seguir à outra, porque partilham o contexto do pedido.
/// </remarks>
public sealed class HistoricoService(IRepositorioHistorico historico) : IHistoricoService
{
    /// <inheritdoc />
    public async Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        var condutor = await historico.ListarViagensComoCondutorAsync(utilizadorId, ct);
        var passageiro = await historico.ListarViagensComoPassageiroAsync(utilizadorId, ct);
        var pedidosEfetuados = await historico.ListarPedidosEfetuadosAsync(utilizadorId, ct);
        var pedidosRecebidos = await historico.ListarPedidosRecebidosAsync(utilizadorId, ct);
        var notificacoes = await historico.ListarNotificacoesAsync(utilizadorId, ct);
        var avisos = await historico.ListarAvisosAsync(utilizadorId, ct);

        return new HistoricoResultado(
            condutor,
            passageiro,
            pedidosEfetuados,
            pedidosRecebidos,
            notificacoes,
            avisos);
    }
}
