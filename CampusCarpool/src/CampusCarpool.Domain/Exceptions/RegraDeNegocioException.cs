namespace CampusCarpool.Domain.Exceptions;

/// <summary>
/// Representa uma violação de uma regra de negócio.
/// </summary>
/// <remarks>
/// Se não for apanhada no controller, o <c>GlobalExceptionHandler</c> da Api converte-a num 409
/// <c>ProblemDetails</c> com um texto genérico (a mensagem da exceção não é enviada ao cliente).
/// É lançada pela <c>UnidadeDeTrabalhoEfCore</c>, quando há um conflito de concorrência ao gravar, e pelo
/// <c>RepositorioAvaliacoes</c>, quando a BD recusa a gravação.
/// Os erros previstos dos serviços saem como <c>Resultado</c> (decisão D3), ou como exceções genéricas do
/// .NET que o controller converte, nas avaliações e nas mensagens (DO-09, DEF-BE-028).
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
