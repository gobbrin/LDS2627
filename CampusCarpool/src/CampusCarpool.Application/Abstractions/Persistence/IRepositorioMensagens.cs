using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Define as operações de persistência das mensagens individuais.</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioMensagens.cs</c>.</remarks>
public interface IRepositorioMensagens
{
    /// <summary>Acrescenta uma mensagem; só fica gravada com a unidade de trabalho.</summary>
    void Adicionar(Mensagem mensagem);

    /// <summary>
    /// Obtém as mensagens trocadas entre dois utilizadores, nos dois sentidos,
    /// ordenadas cronologicamente.
    /// </summary>
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);

    /// <summary>Uma entrada por utilizador com quem houve mensagens, da conversa mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct);
}
