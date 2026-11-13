using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados dos avisos do condutor.</summary>
public interface IRepositorioAvisos
{
    /// <summary>Junta o aviso às alterações pendentes, sem gravar.</summary>
    void Adicionar(Aviso aviso);

    /// <summary>Lista os avisos de uma viagem, do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<Aviso>> ListarPorViagemAsync(Guid viagemId, CancellationToken ct);
}
