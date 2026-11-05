using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados dos utilizadores usado pelos serviços da Application.</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Repositories/RepositorioUtilizadores.cs</c>. O registo, o login e o perfil
/// não passam por aqui: o <c>AutenticacaoController</c> e o <c>UtilizadoresController</c> usam o <c>DbContext</c> diretamente.
/// </remarks>
public interface IRepositorioUtilizadores
{

    /// <summary>Procura um utilizador pelo identificador (só leitura); <c>null</c> se não existir.</summary>
    Task<Utilizador?> ObterPorIdAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Obtém os nomes dos utilizadores indicados, por identificador; os inexistentes ficam de fora.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ObterNomesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}
