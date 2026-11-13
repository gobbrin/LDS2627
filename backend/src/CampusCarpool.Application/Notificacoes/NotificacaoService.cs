using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Notificacoes;

/// <summary>Implementação das notificações pessoais (F006). Ver <see cref="INotificacaoService"/>.</summary>
public sealed class NotificacaoService(
    IRepositorioNotificacoes repositorioNotificacoes,
    IFilaNotificacoesPush filaNotificacoesPush,
    IUnidadeDeTrabalho unidadeDeTrabalho)
    : INotificacaoService
{
    // DC-40: texto mostrado ao utilizador, também no push. Os nomes seguem RF-F006-01 a RF-F006-06.
    private static string Titulo(TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.PedidoRecebido => "Pedido recebido",
        TipoNotificacao.PedidoAceite => "Pedido aceite",
        TipoNotificacao.PedidoRejeitado => "Pedido rejeitado",
        TipoNotificacao.AlteracaoViagem => "Alteração da viagem",
        TipoNotificacao.Aviso => "Aviso do condutor",
        TipoNotificacao.NovaMensagem => "Nova mensagem",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de notificação desconhecido.")
    };

    /// <inheritdoc />
    public Notificacao Registar(Guid utilizadorId, TipoNotificacao tipo, string mensagem, Guid? viagemId = null)
    {
        var notificacao = new Notificacao
        {
            UtilizadorId = utilizadorId,
            ViagemId = viagemId,
            Tipo = tipo,
            Titulo = Titulo(tipo),
            Conteudo = mensagem
        };

        repositorioNotificacoes.Adicionar(notificacao);

        return notificacao;
    }

    /// <inheritdoc />
    public void RegistarAlteracaoViagem(Guid viagemId, Guid condutorId, IEnumerable<Guid> passageiros, Guid origemId, string mensagem)
    {
        var destinatarios = passageiros
            .Append(condutorId)
            .Where(id => id != origemId)
            .Distinct();

        foreach (var destinatario in destinatarios)
            Registar(destinatario, TipoNotificacao.AlteracaoViagem, mensagem, viagemId);
    }

    /// <inheritdoc />
    public void RegistarAviso(Guid viagemId, IEnumerable<Guid> passageiros, string texto)
    {
        foreach (var passageiro in passageiros.Distinct())
        {
            repositorioNotificacoes.Adicionar(new Notificacao
            {
                UtilizadorId = passageiro,
                ViagemId = viagemId,
                Tipo = TipoNotificacao.Aviso,
                Titulo = Titulo(TipoNotificacao.Aviso),
                Conteudo = texto
            });
        }
    }

    /// <inheritdoc />
    public async Task EnviarPushAsync(Notificacao notificacao, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notificacao);

        if (!notificacao.OriginaPush())
            return;

        await filaNotificacoesPush.EnfileirarAsync(
            notificacao.UtilizadorId,
            notificacao.Titulo,
            notificacao.Conteudo,
            notificacao.Tipo.ToString(),
            ct);
    }

    /// <inheritdoc />
    public async Task<PaginaNotificacoes> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoPagina, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanhoPagina, 100);

        var itens = await repositorioNotificacoes.ListarAsync(utilizadorId, pagina, tamanhoPagina, ct);
        var total = await repositorioNotificacoes.ContarAsync(utilizadorId, ct);
        var naoLidas = await repositorioNotificacoes.ContarNaoLidasAsync(utilizadorId, ct);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);

        var dtos = itens
            .Select(n => new NotificacaoPessoal(
                n.Id, n.UtilizadorId, n.ViagemId, n.Tipo.ToString(), n.Titulo, n.Conteudo, n.LidaEm, n.CriadaEm))
            .ToList();

        return new PaginaNotificacoes(dtos, pagina, tamanhoPagina, total, totalPaginas, naoLidas);
    }

    /// <inheritdoc />
    public async Task<Resultado> MarcarComoLidaAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct)
    {
        // Só encontra a notificação se pertencer ao utilizador.
        var notificacao = await repositorioNotificacoes.ObterAsync(notificacaoId, utilizadorId, ct);

        if (notificacao is null)
            return new Erro(TipoErro.NaoEncontrado, "Notificação não encontrada.");

        notificacao.MarcarComoLida(DateTime.UtcNow);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<int> MarcarTodasComoLidasAsync(Guid utilizadorId, CancellationToken ct)
    {
        var notificacoes = await repositorioNotificacoes.ListarNaoLidasAsync(utilizadorId, ct);

        if (notificacoes.Count == 0)
            return 0;

        var dataLeitura = DateTime.UtcNow;

        foreach (var notificacao in notificacoes)
            notificacao.MarcarComoLida(dataLeitura);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return notificacoes.Count;
    }
}
