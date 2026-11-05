namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Abstrai a confirmação das alterações pendentes.</summary>
/// <remarks>
/// Implementado por <c>UnidadeDeTrabalhoEfCore</c>, que chama <c>SaveChanges()</c> e converte um conflito de
/// concorrência (coluna <c>Versao</c> da viagem) numa <c>RegraDeNegocioException</c>, que chega ao cliente
/// como 409. A gravação é síncrona e sem <c>CancellationToken</c> (AP-08).
/// </remarks>
public interface IUnidadeDeTrabalho
{
    /// <summary>Grava todas as alterações pendentes das entidades rastreadas.</summary>
    /// <exception cref="CampusCarpool.Domain.Exceptions.RegraDeNegocioException">Outro pedido alterou a mesma viagem entretanto.</exception>
    void Confirmar();
}
