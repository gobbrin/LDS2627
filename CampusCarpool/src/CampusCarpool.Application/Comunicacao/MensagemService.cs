using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
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
    // P-14: tamanho máximo do texto.
    private const int TamanhoMaximoConteudo = 1000;

    /// <inheritdoc />
    /// <remarks>
    /// Valida os participantes, guarda a mensagem e cria a notificação. São duas gravações separadas: se a
    /// notificação falhar, a mensagem já ficou gravada (AP-02).
    /// </remarks>
    public async Task<Resultado<MensagemIndividual>> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct)
    {
        if (remetenteId == destinatarioId)
            return new Erro(TipoErro.DadosInvalidos, "Não é possível enviar mensagens para si próprio.");

        // O conteúdo valida-se antes de consultar a base de dados.
        if (string.IsNullOrWhiteSpace(conteudo) || conteudo.Trim().Length > TamanhoMaximoConteudo)
            return new Erro(TipoErro.DadosInvalidos, "A mensagem deve conter entre 1 e 1000 caracteres.");

        var remetente = await repositorioUtilizadores.ObterPorIdAsync(remetenteId, ct);

        if (remetente is null || !remetente.EmailConfirmado)
            return new Erro(TipoErro.DadosInvalidos, "O remetente não existe ou não tem a conta confirmada.");

        var destinatario = await repositorioUtilizadores.ObterPorIdAsync(destinatarioId, ct);

        if (destinatario is null || !destinatario.EmailConfirmado)
            return new Erro(TipoErro.DadosInvalidos, "O destinatário não existe ou não tem a conta confirmada.");

        var mensagem = new Mensagem
        {
            RemetenteId = remetenteId,
            DestinatarioId = destinatarioId,
            Conteudo = conteudo.Trim(),
            EnviadoEm = DateTime.UtcNow
        };

        await repositorioMensagens.AdicionarAsync(mensagem, ct);

        await notificacaoService.CriarAsync(
            destinatarioId,
            TipoNotificacao.NovaMensagem,
            $"Recebeste uma nova mensagem de {remetente.Nome}.",
            ct);

        return Dto(mensagem);
    }

    /// <inheritdoc />
    public async Task<Resultado<List<MensagemIndividual>>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct)
    {
        if (utilizadorId == outroUtilizadorId)
            return new Erro(TipoErro.DadosInvalidos, "Não é possível consultar uma conversa consigo próprio.");

        var outroUtilizador = await repositorioUtilizadores.ObterPorIdAsync(outroUtilizadorId, ct);

        if (outroUtilizador is null || !outroUtilizador.EmailConfirmado)
            return new Erro(TipoErro.DadosInvalidos, "O utilizador não existe ou não tem a conta confirmada.");

        // O repositório devolve apenas mensagens entre estes participantes.
        var mensagens = await repositorioMensagens.ObterConversaAsync(utilizadorId, outroUtilizadorId, ct);

        return mensagens.Select(Dto).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct) =>
        repositorioMensagens.ListarConversasAsync(utilizadorId, ct);

    private static MensagemIndividual Dto(Mensagem mensagem) => new(
        mensagem.Id,
        mensagem.ViagemId,
        mensagem.RemetenteId,
        mensagem.DestinatarioId,
        mensagem.Conteudo,
        mensagem.EnviadoEm);
}
