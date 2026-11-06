namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Abstrai a confirmação das alterações pendentes.</summary>
/// <remarks>
/// Implementado por <c>UnidadeDeTrabalhoEfCore</c>, que grava no contexto do pedido e converte um conflito de
/// concorrência (colunas <c>Versao</c>) numa <c>RegraDeNegocioException</c>, que chega ao cliente como 409.
/// </remarks>
public interface IUnidadeDeTrabalho
{
    /// <summary>Grava todas as alterações pendentes das entidades rastreadas.</summary>
    /// <param name="ct">Cancelamento.</param>
    /// <exception cref="CampusCarpool.Domain.Exceptions.RegraDeNegocioException">Outro pedido alterou a mesma viagem ou participação entretanto.</exception>
    Task ConfirmarAsync(CancellationToken ct);

    /// <summary>Grava como <see cref="ConfirmarAsync"/>, mas devolve <c>false</c> se a BD recusar, por exemplo por um índice único.</summary>
    /// <param name="ct">Cancelamento.</param>
    /// <exception cref="CampusCarpool.Domain.Exceptions.RegraDeNegocioException">Outro pedido alterou a mesma viagem ou participação entretanto.</exception>
    Task<bool> TentarConfirmarAsync(CancellationToken ct);

    /// <summary>Esquece as alterações por gravar e as entidades rastreadas, para isolar uma operação da seguinte.</summary>
    void DescartarAlteracoes();
}
