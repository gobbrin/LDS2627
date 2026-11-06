namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Abstrai a confirmação das alterações pendentes.</summary>
/// <remarks>
/// Implementado por <c>UnidadeDeTrabalhoEfCore</c>, que grava no contexto do pedido e converte um conflito de
/// concorrência (coluna <c>Versao</c> da viagem) numa <c>RegraDeNegocioException</c>, que chega ao cliente como 409.
/// A versão síncrona, sem <c>CancellationToken</c>, ainda é a que o <c>ParticipacaoService</c> usa (AP-08).
/// </remarks>
public interface IUnidadeDeTrabalho
{
    /// <summary>Grava todas as alterações pendentes das entidades rastreadas.</summary>
    /// <exception cref="CampusCarpool.Domain.Exceptions.RegraDeNegocioException">Outro pedido alterou a mesma viagem entretanto.</exception>
    void Confirmar();

    /// <summary>Grava todas as alterações pendentes das entidades rastreadas.</summary>
    /// <param name="ct">Cancelamento.</param>
    /// <exception cref="CampusCarpool.Domain.Exceptions.RegraDeNegocioException">Outro pedido alterou a mesma viagem entretanto.</exception>
    Task ConfirmarAsync(CancellationToken ct);

    /// <summary>Esquece as alterações por gravar e as entidades rastreadas, para isolar uma operação da seguinte.</summary>
    void DescartarAlteracoes();
}
