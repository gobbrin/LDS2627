using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
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
        int pagina,
        int tamanho,
        CancellationToken ct)
    {
        // Cada lista lê só a página pedida e conta o seu total (D-4).
        PaginaDe<Viagem> comoCondutor = await historico.ListarViagensComoCondutorAsync(utilizadorId, pagina, tamanho, ct);
        PaginaDe<Participacao> comoPassageiro = await historico.ListarParticipacoesComoPassageiroAsync(utilizadorId, pagina, tamanho, ct);
        PaginaDe<Participacao> efetuados = await historico.ListarPedidosEfetuadosAsync(utilizadorId, pagina, tamanho, ct);
        PaginaDe<Participacao> recebidos = await historico.ListarPedidosRecebidosAsync(utilizadorId, pagina, tamanho, ct);
        PaginaDe<Notificacao> notificacoes = await historico.ListarNotificacoesAsync(utilizadorId, pagina, tamanho, ct);
        PaginaDe<Aviso> avisos = await historico.ListarAvisosAsync(utilizadorId, pagina, tamanho, ct);

        var totais = new TotaisHistorico(
            comoCondutor.Total,
            comoPassageiro.Total,
            efetuados.Total,
            recebidos.Total,
            notificacoes.Total,
            avisos.Total);

        return new HistoricoResultado(
            pagina,
            tamanho,
            totais,
            comoCondutor.Itens.Select(ViagemComoCondutor).ToList(),
            comoPassageiro.Itens.Select(ViagemComoPassageiro).ToList(),
            efetuados.Itens.Select(Pedido).ToList(),
            recebidos.Itens.Select(Pedido).ToList(),
            notificacoes.Itens.Select(NotificacaoDoHistorico).ToList(),
            avisos.Itens.Select(AvisoDoHistorico).ToList());
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
        notificacao.RemetenteId,
        notificacao.Tipo.ToString(),
        notificacao.Titulo,
        notificacao.Conteudo,
        notificacao.CriadaEm,
        notificacao.LidaEm);

    private static AvisoHistorico AvisoDoHistorico(Aviso aviso) => new(aviso.Id, aviso.ViagemId, aviso.Conteudo, aviso.CriadoEm);
}
