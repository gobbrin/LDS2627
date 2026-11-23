using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Define as operações de persistência das mensagens individuais.</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioMensagens.cs</c>.</remarks>
public interface IRepositorioMensagens
{
    /// <summary>Acrescenta uma mensagem; só fica gravada com a unidade de trabalho.</summary>
    void Adicionar(Mensagem mensagem);

    /// <summary>Obtém uma mensagem pelo id, sem seguimento; <c>null</c> se não existir.</summary>
    Task<Mensagem?> ObterPorIdAsync(Guid mensagemId, CancellationToken ct);

    /// <summary>
    /// Uma página da conversa entre dois utilizadores, nos dois sentidos (BE-13): as <paramref name="tamanho"/>
    /// mensagens mais recentes anteriores a <paramref name="antesDe"/>, por ordem cronológica.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="outroUtilizadorId">Outro participante.</param>
    /// <param name="antesDe">Mensagem de referência (cursor), já validada; <c>null</c> para a página mais recente.</param>
    /// <param name="tamanho">Número máximo de mensagens.</param>
    /// <param name="ct">Cancelamento.</param>
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync(
        Guid utilizadorId,
        Guid outroUtilizadorId,
        Mensagem? antesDe,
        int tamanho,
        CancellationToken ct);

    /// <summary>
    /// A última mensagem de cada conversa do utilizador (uma por interlocutor), da mais recente para a mais antiga.
    /// </summary>
    /// <remarks>Devolve as entidades, sem seguimento; o resumo da conversa monta-se no serviço (BE-07).</remarks>
    Task<IReadOnlyList<Mensagem>> ListarUltimasMensagensAsync(Guid utilizadorId, CancellationToken ct);
}
