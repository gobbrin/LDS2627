namespace CampusCarpool.Domain.Exceptions;

/// <summary>
/// Representa uma violação de uma regra de negócio.
/// </summary>
/// <remarks>
/// O <c>GlobalExceptionHandler</c> da Api converte-a num 409 <c>ProblemDetails</c>, com a mensagem da exceção.
/// É lançada pelas entidades, numa transição inválida, e pela <c>UnidadeDeTrabalhoEfCore</c>, quando há um conflito de
/// concorrência ao gravar. Os erros
/// previstos dos serviços saem como <c>Resultado</c> (decisão D3).
/// </remarks>
public sealed class RegraDeNegocioException : Exception
{
    /// <summary>Cria a exceção com a mensagem a apresentar ao utilizador.</summary>
    /// <param name="message">Descrição da regra violada, em português.</param>
    public RegraDeNegocioException(string message) : base(message)
    {
    }

    /// <summary>Cria a exceção com a mensagem e a exceção que a causou.</summary>
    /// <param name="message">Descrição da regra violada, em português.</param>
    /// <param name="innerException">Exceção original.</param>
    public RegraDeNegocioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
