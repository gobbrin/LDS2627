using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Recálculo da rota guardada de uma viagem (RF-F008-05). Implementado na Api, que tem o cliente do Serviço de Mapas.</summary>
public interface IRotaViagemService
{
    /// <summary>Recalcula a rota com as participações indicadas e escreve-a na viagem, sem gravar.</summary>
    /// <param name="viagem">Viagem a atualizar.</param>
    /// <param name="aceites">Participações que a rota deve incluir.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns><c>false</c>, sem alterar a viagem, se o Serviço de Mapas estiver indisponível (P-19).</returns>
    Task<bool> RecalcularAsync(Viagem viagem, IReadOnlyCollection<Participacao> aceites, CancellationToken ct);
}
