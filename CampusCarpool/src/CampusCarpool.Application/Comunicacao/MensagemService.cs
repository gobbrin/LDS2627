using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Implementação das mensagens individuais (F005). Ver <see cref="IMensagemService"/>.</summary>
public sealed class MensagemService(
    IRepositorioMensagens repositorioMensagens,
    IRepositorioUtilizadores repositorioUtilizadores,
    INotificacaoService notificacaoService) : IMensagemService
{

    /// <inheritdoc />
    /// <remarks>
    /// Valida os participantes, guarda a mensagem e cria a notificação. São duas gravações separadas: se a
    /// notificação falhar, a mensagem já ficou gravada (AP-02).
    /// </remarks>
    public async Task<Mensagem> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct)
    {
        //Impede o envio de mensagens para a própria conta.
        if (remetenteId == destinatarioId)
        {
            throw new ArgumentException("Não é possível enviar mensagens para si próprio.");
        }

        //Valida o conteúdo antes de consultar a base de dados.
        if (string.IsNullOrWhiteSpace(conteudo) || conteudo.Trim().Length > 1000)
        {
            throw new ArgumentException("A mensagem deve conter entre 1 e 1000 caracteres.");
        }

        //Confirma a existência e o estado das contas envolvidas.
        var remetente = await repositorioUtilizadores.ObterPorIdAsync(remetenteId, ct);

        if (remetente is null || !remetente.EmailConfirmado)
        {
            throw new InvalidOperationException("O remetente não existe ou não tem a conta confirmada.");
        }

        var destinatario = await repositorioUtilizadores.ObterPorIdAsync(destinatarioId, ct);

        if (destinatario is null || !destinatario.EmailConfirmado)
        {
            throw new InvalidOperationException("O destinatário não existe ou não tem a conta confirmada.");
        }

        //Cria a mensagem com o conteúdo normalizado e a data atual.
        var mensagem = new Mensagem
        {
            RemetenteId = remetenteId,
            DestinatarioId = destinatarioId,
            Conteudo = conteudo.Trim(),
            EnviadoEm = DateTime.UtcNow
        };

        //Persiste a mensagem antes de criar a notificação.
        await repositorioMensagens.AdicionarAsync(mensagem, ct);

        //Notifica o destinatário de que recebeu uma nova mensagem.
        await notificacaoService.CriarAsync(
            destinatarioId,
            TipoNotificacao.NovaMensagem,
            $"Recebeste uma nova mensagem de {remetente.Nome}.",
            ct);

        return mensagem;
    }

    /// <inheritdoc />
    /// <remarks>Devolve o histórico da conversa entre os dois participantes, sem paginação (AP-10).</remarks>
    public async Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct)
    {
        //Impede a consulta de uma conversa consigo próprio.
        if (utilizadorId == outroUtilizadorId)
        {
            throw new ArgumentException("Não é possível consultar uma conversa consigo próprio.");
        }

        //Confirma que o outro participante existe e tem a conta confirmada.
        var outroUtilizador = await repositorioUtilizadores.ObterPorIdAsync(outroUtilizadorId, ct);

        if (outroUtilizador is null || !outroUtilizador.EmailConfirmado)
        {
            throw new InvalidOperationException("O utilizador não existe ou não tem a conta confirmada.");
        }

        //O repositório devolve apenas mensagens entre estes participantes.
        return await repositorioMensagens.ObterConversaAsync(
            utilizadorId,
            outroUtilizadorId,
            ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct) =>
        repositorioMensagens.ListarConversasAsync(utilizadorId, ct);
}
