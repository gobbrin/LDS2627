using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Teste que precisa de um SQL Server real. Só corre quando a variável de ambiente
/// <see cref="VariavelLigacao"/> tem uma connection string; caso contrário fica marcado
/// como saltado (com o motivo no relatório) em vez de falhar.
/// </summary>
/// <remarks>
/// Exemplos de valor: <c>Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;</c>
/// (Windows com LocalDB) ou <c>Server=localhost,1433;User Id=sa;Password=…;TrustServerCertificate=True;</c>
/// (contentor). O nome da base de dados é acrescentado por cada teste.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string VariavelLigacao = "CAMPUSCARPOOL_TESTES_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ObterConnectionString()))
        {
            Skip = $"Sem SQL Server: definir {VariavelLigacao} com a connection string do servidor.";
        }
    }

    public static string? ObterConnectionString() =>
        Environment.GetEnvironmentVariable(VariavelLigacao);
}
