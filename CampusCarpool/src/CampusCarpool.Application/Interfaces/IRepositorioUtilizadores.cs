using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

public interface IRepositorioUtilizadores {
    
    //Procura um utilizador pelo identificador.
    Task<Utilizador?> ObterPorIdAsync(Guid utilizadorId, CancellationToken ct);
}