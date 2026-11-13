using CampusCarpool.Domain.Entities;
namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados dos dispositivos Android registados para push (RF-F006-10).</summary>
/// <remarks>Implementado por <c>Infrastructure/Repositories/RepositorioDispositivo.cs</c>.</remarks>
public interface IRepositorioDispositivos
{
    /// <summary>Lista os dispositivos ativos do utilizador (destinos de um push).</summary>
    Task<IReadOnlyList<Dispositivo>> ListarAtivosAsync(Guid utilizadorId, CancellationToken ct);
    /// <summary>
    /// Regista o token FCM para o utilizador. Se o token já existir (noutra conta, por exemplo), passa a
    /// pertencer a este utilizador e fica ativo: um token identifica uma instalação e só tem um dono.
    /// </summary>
    Task RegistarAsync(Guid utilizadorId, string token, CancellationToken ct);
    /// <summary>Desativa o token, se pertencer ao utilizador; não faz nada caso contrário.</summary>
    Task DesativarAsync(Guid utilizadorId, string token, CancellationToken ct);
    /// <summary>Desativa um token que o Firebase indicou como inválido, seja de quem for.</summary>
    Task DesativarTokenInvalidoAsync(string token, CancellationToken ct);
}
