using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Services;

public sealed class NotificacaoService(
    IRepositorioNotificacoes repositorioNotificacoes,
    IFilaNotificacoesPush filaNotificacoesPush)
    : INotificacaoService
{
    // Cria uma notificação e delega a sua persistência ao repositório.
    public async Task<Notificacao> CriarAsync(Guid utilizadorId, TipoNotificacao tipo, string mensagem, CancellationToken ct)
    {
        // Constrói a notificação para o utilizador destinatário.
        var notificacao = new Notificacao
        {
            UtilizadorId = utilizadorId,
            Tipo = tipo,
            Titulo = tipo.ToString(),
            Conteudo = mensagem
        };

        // Guarda a notificação antes de colocar o envio push na fila.
        await repositorioNotificacoes.AdicionarAsync(notificacao, ct);

        // Só os três eventos de pedidos geram push Android (F006).
        if (tipo is TipoNotificacao.PedidoRecebido
            or TipoNotificacao.PedidoAceite
            or TipoNotificacao.PedidoRejeitado)
        {
            await filaNotificacoesPush.EnfileirarAsync(
                utilizadorId,
                notificacao.Titulo,
                mensagem,
                tipo.ToString(),
                ct);
        }

        return notificacao;
    }

    // Devolve uma página de notificações pessoais.
    public async Task<PaginaNotificacoes> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);

        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoPagina, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanhoPagina, 100);

        var itens = await repositorioNotificacoes.ListarAsync(utilizadorId, pagina, tamanhoPagina, ct);

        var total = await repositorioNotificacoes.ContarAsync(utilizadorId, ct);

        var naoLidas = await repositorioNotificacoes.ContarNaoLidasAsync(utilizadorId, ct);

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);

        return new PaginaNotificacoes(itens, pagina, tamanhoPagina, total, totalPaginas, naoLidas);
    }

    // Marca uma notificação como lida apenas se pertencer ao utilizador.
    public async Task<bool> MarcarComoLidaAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct)
    {
        var notificacao = await repositorioNotificacoes.ObterAsync(notificacaoId, utilizadorId, ct);

        if (notificacao is null)
        {
            return false;
        }

        if (notificacao.LidaEm is not null)
        {
            return true;
        }

        notificacao.LidaEm = DateTime.UtcNow;

        await repositorioNotificacoes.GuardarAlteracoesAsync(ct);

        return true;
    }

    // Marca todas as notificações não lidas do utilizador como lidas.
    public async Task<int> MarcarTodasComoLidasAsync(Guid utilizadorId, CancellationToken ct)
    {
        var notificacoes = await repositorioNotificacoes.ListarNaoLidasAsync(utilizadorId, ct);

        if (notificacoes.Count == 0)
        {
            return 0;
        }

        var dataLeitura = DateTime.UtcNow;

        foreach (var notificacao in notificacoes)
        {
            notificacao.LidaEm = dataLeitura;
        }

        await repositorioNotificacoes.GuardarAlteracoesAsync(ct);

        return notificacoes.Count;
    }
}
