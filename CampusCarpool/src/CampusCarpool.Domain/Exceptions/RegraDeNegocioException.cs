namespace CampusCarpool.Domain.Exceptions;

// Representa uma violação de uma regra de negócio.
public sealed class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string message) : base(message)
    {
    }

    public RegraDeNegocioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

