
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Services;

/// <summary>Implementação das notificações pessoais (F006). Ver <see cref="INotificacaoService"/>.</summary>
public sealed class NotificacaoService(
    IRepositorioNotificacoes repositorioNotificacoes,
    IFilaNotificacoesPush filaNotificacoesPush)
    : INotificacaoService
{
    /// <inheritdoc />
    /// <remarks>
    /// Cria uma notificação e delega a persistência ao repositório.
    /// Não preenche <c>Notificacao.ViagemId</c>: só as notificações de aviso (criadas pelo <c>AvisoService</c>)
    /// ficam ligadas à viagem.
    /// </remarks>
    public async Task<Notificacao> CriarAsync(
        Guid utilizadorId,
        TipoNotificacao tipo,
        string mensagem,
        CancellationToken ct)
    {
        var notificacao = new Notificacao
        {
            UtilizadorId = utilizadorId,
            Tipo = tipo,
            // DC-40 / AP-06: o título é o nome do enum (ex.: "PedidoAceite"), também no push; fica para m4.
            Titulo = tipo.ToString(),
            Conteudo = mensagem
        };

        // Guarda a notificação antes de colocar o push na fila.
        await repositorioNotificacoes.AdicionarAsync(notificacao, ct);

        // Apenas estes três eventos geram notificações push Android.
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

    /// <inheritdoc />
    public async Task NotificarAlteracaoViagemAsync(
        Guid condutorId,
        IEnumerable<Guid> passageiros,
        Guid origemId,
        string mensagem,
        CancellationToken ct)
    {
        var destinatarios = passageiros
            .Append(condutorId)
            .Where(id => id != origemId)
            .Distinct()
            .ToList();

        foreach (var destinatario in destinatarios)
        {
            await CriarAsync(destinatario, TipoNotificacao.AlteracaoViagem, mensagem, ct);
        }
    }

    // Devolve uma página de notificações pessoais.
    /// <inheritdoc />
    public async Task<PaginaNotificacoes> ListarAsync(
        Guid utilizadorId,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoPagina, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanhoPagina, 100);

        var itens = await repositorioNotificacoes.ListarAsync(
            utilizadorId, pagina, tamanhoPagina, ct);

        var total = await repositorioNotificacoes.ContarAsync(
            utilizadorId, ct);

        var naoLidas = await repositorioNotificacoes.ContarNaoLidasAsync(
            utilizadorId, ct);

        var totalPaginas = (int)Math.Ceiling(
            total / (double)tamanhoPagina);

        return new PaginaNotificacoes(
            itens,
            pagina,
            tamanhoPagina,
            total,
            totalPaginas,
            naoLidas);
    }

    // Marca uma notificação como lida apenas se pertencer ao utilizador.
    /// <inheritdoc />
    public async Task<bool> MarcarComoLidaAsync(
        Guid notificacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var notificacao = await repositorioNotificacoes.ObterAsync(
            notificacaoId, utilizadorId, ct);

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

    // Marca todas as notificações não lidas como lidas.
    /// <inheritdoc />
    public async Task<int> MarcarTodasComoLidasAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        var notificacoes = await repositorioNotificacoes.ListarNaoLidasAsync(
            utilizadorId, ct);

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

