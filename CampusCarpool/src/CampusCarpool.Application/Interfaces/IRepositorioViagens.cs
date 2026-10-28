using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

public interface IRepositorioViagens
{
    // Obtém uma viagem pelo identificador.
    // A implementação deve permitir atualizar a entidade obtida.
    Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct);
    
}