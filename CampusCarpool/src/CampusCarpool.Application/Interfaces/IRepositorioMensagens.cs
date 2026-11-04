using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Define as operações de persistência das mensagens individuais.</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioMensagens.cs</c>.</remarks>
public interface IRepositorioMensagens
{

    /// <summary>Guarda uma mensagem na base de dados (grava logo, com <c>SaveChangesAsync</c>).</summary>
    Task AdicionarAsync(Mensagem mensagem, CancellationToken ct);

    /// <summary>
    /// Obtém as mensagens trocadas entre dois utilizadores, nos dois sentidos,
    /// ordenadas cronologicamente.
    /// </summary>
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);
}
