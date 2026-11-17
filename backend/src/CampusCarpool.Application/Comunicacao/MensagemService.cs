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
    INotificacaoService notificacaoService,
    IUnidadeDeTrabalho unidadeDeTrabalho) : IMensagemService
{
    /// <summary>Mensagens por página da conversa, quando o cliente não indica o tamanho (BE-13).</summary>
    public const int TamanhoPorOmissao = 50;

    /// <inheritdoc />
    /// <remarks>A mensagem e a notificação são gravadas juntas (DC-37). <c>NovaMensagem</c> não gera push (RF-F006-12).</remarks>
    public async Task<Resultado<MensagemIndividual>> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct)
    {
        if (remetenteId == destinatarioId)
            return new Erro(TipoErro.DadosInvalidos, "Não é possível enviar mensagens para si próprio.");

        // O conteúdo valida-se antes de consultar a base de dados.
        if (string.IsNullOrWhiteSpace(conteudo) || conteudo.Trim().Length > Mensagem.TamanhoMaximoConteudo)
            return new Erro(TipoErro.DadosInvalidos, "A mensagem deve conter entre 1 e 1000 caracteres.");

        // O remetente vem do token: a conta existe e está confirmada, porque o login recusa as outras (BE-24).
        // Lê-se só pelo nome, para a notificação.
        var remetente = await repositorioUtilizadores.ObterPorIdAsync(remetenteId, ct);

        if (remetente is null)
            return new Erro(TipoErro.DadosInvalidos, "O remetente não existe.");

        var destinatario = await repositorioUtilizadores.ObterPorIdAsync(destinatarioId, ct);

        if (destinatario is null || !destinatario.EmailConfirmado)
            return new Erro(TipoErro.DadosInvalidos, "O destinatário não existe ou não tem a conta confirmada.");

        var mensagem = Mensagem.Criar(remetenteId, destinatarioId, conteudo, DateTime.UtcNow);

        repositorioMensagens.Adicionar(mensagem);

        notificacaoService.Registar(
            destinatarioId,
            TipoNotificacao.NovaMensagem,
            $"Recebeste uma nova mensagem de {remetente.Nome}.");

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Dto(mensagem);
    }

    /// <inheritdoc />
    public async Task<Resultado<List<MensagemIndividual>>> ObterConversaAsync(
        Guid utilizadorId,
        Guid outroUtilizadorId,
        Guid? antesDe,
        int tamanho,
        CancellationToken ct)
    {
        if (utilizadorId == outroUtilizadorId)
            return new Erro(TipoErro.DadosInvalidos, "Não é possível consultar uma conversa consigo próprio.");

        var outroUtilizador = await repositorioUtilizadores.ObterPorIdAsync(outroUtilizadorId, ct);

        if (outroUtilizador is null || !outroUtilizador.EmailConfirmado)
            return new Erro(TipoErro.DadosInvalidos, "O utilizador não existe ou não tem a conta confirmada.");

        // O cursor tem de ser uma mensagem desta conversa: assim ninguém usa o id de uma conversa alheia (RF-F005-04).
        Mensagem? referencia = null;

        if (antesDe is Guid mensagemId)
        {
            referencia = await repositorioMensagens.ObterPorIdAsync(mensagemId, ct);

            if (referencia is null || !DaConversa(referencia, utilizadorId, outroUtilizadorId))
                return new Erro(TipoErro.DadosInvalidos, "A mensagem indicada em antesDe não pertence a esta conversa.");
        }

        // O repositório devolve apenas mensagens entre estes participantes.
        var mensagens = await repositorioMensagens.ObterConversaAsync(utilizadorId, outroUtilizadorId, referencia, tamanho, ct);

        return mensagens.Select(Dto).ToList();
    }

    // A mensagem foi trocada entre estes dois utilizadores, num sentido ou no outro.
    private static bool DaConversa(Mensagem mensagem, Guid utilizadorId, Guid outroUtilizadorId) =>
        (mensagem.RemetenteId == utilizadorId && mensagem.DestinatarioId == outroUtilizadorId)
        || (mensagem.RemetenteId == outroUtilizadorId && mensagem.DestinatarioId == utilizadorId);

    /// <inheritdoc />
    /// <remarks>
    /// A base de dados devolve só a última mensagem de cada conversa (uma linha por interlocutor, não por mensagem);
    /// a página corta-se depois, aqui, porque duas mensagens no mesmo instante só se desempatam em memória.
    /// </remarks>
    public async Task<PaginaConversas> ListarConversasAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct)
    {
        IReadOnlyList<Mensagem> ultimas = await repositorioMensagens.ListarUltimasMensagensAsync(utilizadorId, ct);

        // Só as conversas da página pedida (D-4).
        List<Mensagem> daPagina = ultimas.Skip(Paginacao.Saltar(pagina, tamanho)).Take(tamanho).ToList();

        // O interlocutor é quem não é o utilizador autenticado.
        var outrosIds = new List<Guid>();

        foreach (Mensagem mensagem in daPagina)
            outrosIds.Add(Interlocutor(mensagem, utilizadorId));

        IReadOnlyDictionary<Guid, string> nomes = await repositorioUtilizadores.ObterNomesAsync(outrosIds, ct);

        // O resumo monta-se aqui, não no repositório (BE-07).
        var conversas = new List<ResumoConversa>();

        foreach (Mensagem mensagem in daPagina)
        {
            Guid outroId = Interlocutor(mensagem, utilizadorId);
            conversas.Add(new ResumoConversa(outroId, nomes[outroId], mensagem.Id, mensagem.Conteudo, mensagem.EnviadoEm));
        }

        return new PaginaConversas(ultimas.Count, pagina, tamanho, conversas);
    }

    // O outro participante de uma mensagem em que o utilizador é remetente ou destinatário.
    private static Guid Interlocutor(Mensagem mensagem, Guid utilizadorId) =>
        mensagem.RemetenteId == utilizadorId ? mensagem.DestinatarioId : mensagem.RemetenteId;

    private static MensagemIndividual Dto(Mensagem mensagem) => new(
        mensagem.Id,
        mensagem.RemetenteId,
        mensagem.DestinatarioId,
        mensagem.Conteudo,
        mensagem.EnviadoEm);
}
