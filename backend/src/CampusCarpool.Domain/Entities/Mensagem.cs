using CampusCarpool.Domain.Exceptions;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Mensagem individual entre dois utilizadores confirmados (F005, RF-F005-01).
/// </summary>
/// <remarks>
/// Uma conversa é o conjunto das mensagens trocadas entre o mesmo par de utilizadores; só os dois
/// intervenientes lhe têm acesso (RF-F005-04). Limite de 1000 caracteres (P-14), garantido em <see cref="Criar"/>
/// (o <c>MensagemService</c> verifica antes, para devolver a mensagem ao utilizador). Cada mensagem gera uma
/// notificação <c>NovaMensagem</c>, sem push (RF-F006-12).
/// </remarks>
public sealed class Mensagem
{
    /// <summary>Tamanho máximo do texto (P-14).</summary>
    public const int TamanhoMaximoConteudo = 1000;

    /// <summary>Identificador da mensagem.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Autor da mensagem.</summary>
    public Guid RemetenteId { get; set; }

    /// <summary>Destinatário da mensagem.</summary>
    public Guid DestinatarioId { get; set; }

    /// <summary>Texto da mensagem.</summary>
    public string Conteudo { get; set; } = "";

    /// <summary>Data de envio (UTC); a conversa é ordenada por este campo (RF-F005-09).</summary>
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Cria uma mensagem entre dois utilizadores diferentes, com o texto sem espaços nas pontas (RF-F005-01).</summary>
    /// <param name="remetenteId">Autor.</param>
    /// <param name="destinatarioId">Destinatário; não pode ser o autor.</param>
    /// <param name="conteudo">Texto, de 1 a 1000 caracteres depois de tirar os espaços (P-14).</param>
    /// <param name="agora">Momento do envio (UTC).</param>
    /// <returns>A mensagem, por gravar.</returns>
    /// <exception cref="RegraDeNegocioException">O destinatário é o autor ou o texto está vazio ou é longo.</exception>
    public static Mensagem Criar(Guid remetenteId, Guid destinatarioId, string conteudo, DateTime agora)
    {
        if (remetenteId == destinatarioId)
            throw new RegraDeNegocioException("Não é possível enviar mensagens para si próprio.");

        string texto = conteudo?.Trim() ?? "";

        if (texto.Length == 0 || texto.Length > TamanhoMaximoConteudo)
            throw new RegraDeNegocioException("A mensagem deve conter entre 1 e 1000 caracteres.");

        return new Mensagem
        {
            RemetenteId = remetenteId,
            DestinatarioId = destinatarioId,
            Conteudo = texto,
            EnviadoEm = agora
        };
    }
}
