using CampusCarpool.Domain.Entities;
namespace CampusCarpool.Application.Interfaces;
public interface IRepositorioDispositivos
{
    Task<IReadOnlyList<Dispositivo>> ListarAtivosAsync(Guid utilizadorId, CancellationToken ct);
    Task RegistarAsync(Guid utilizadorId, string token, CancellationToken ct);
    Task DesativarAsync(Guid utilizadorId, string token, CancellationToken ct);
    Task DesativarTokenInvalidoAsync(string token, CancellationToken ct);
}
