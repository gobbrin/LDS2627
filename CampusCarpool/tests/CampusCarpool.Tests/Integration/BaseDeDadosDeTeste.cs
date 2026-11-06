using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CampusCarpool.Tests;

/// <summary>Escolhe a base de dados dos testes: SQLite em memória por omissão, ou SQL Server real com as migrations.</summary>
/// <remarks>
/// Com <c>CAMPUSCARPOOL_TESTES_BD=SqlServer</c> e <c>CAMPUSCARPOOL_TESTES_SQLSERVER</c> definidas, toda a suite corre em
/// SQL Server. Cada <see cref="SqliteConnection"/> de uma fixture passa a identificar uma BD com nome único, com o
/// esquema das migrations, apagada quando a ligação é fechada. Os testes não mudam: só chamam
/// <see cref="UseBaseDeTeste(DbContextOptionsBuilder, SqliteConnection)"/> e <see cref="CriarEsquemaDeTeste"/>.
/// </remarks>
public static class BaseDeDadosDeTeste
{
    public const string VariavelModo = "CAMPUSCARPOOL_TESTES_BD";

    private static readonly ConditionalWeakTable<SqliteConnection, string> Bases = [];

    // Em SQL Server, dois CREATE DATABASE ao mesmo tempo podem falhar no bloqueio da base model.
    private static readonly Lock CriacaoDeBases = new();

    /// <summary>Indica se a suite está a correr em SQL Server.</summary>
    public static bool EmSqlServer { get; } =
        string.Equals(Environment.GetEnvironmentVariable(VariavelModo), "SqlServer", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Integration.SqlServerFactAttribute.ObterConnectionString());

    /// <summary>Configura o contexto para a base de dados associada à ligação da fixture.</summary>
    public static DbContextOptionsBuilder<TContext> UseBaseDeTeste<TContext>(
        this DbContextOptionsBuilder<TContext> opcoes,
        SqliteConnection ligacao)
        where TContext : DbContext =>
        EmSqlServer ? opcoes.UseSqlServer(CadeiaDe(ligacao)) : opcoes.UseSqlite(ligacao);

    /// <inheritdoc cref="UseBaseDeTeste{TContext}"/>
    public static DbContextOptionsBuilder UseBaseDeTeste(this DbContextOptionsBuilder opcoes, SqliteConnection ligacao) =>
        EmSqlServer ? opcoes.UseSqlServer(CadeiaDe(ligacao)) : opcoes.UseSqlite(ligacao);

    /// <summary>Cria o esquema: o do modelo em SQLite, o das migrations em SQL Server.</summary>
    public static void CriarEsquemaDeTeste(this DatabaseFacade baseDeDados)
    {
        if (EmSqlServer) baseDeDados.Migrate();
        else baseDeDados.EnsureCreated();
    }

    /// <inheritdoc cref="CriarEsquemaDeTeste"/>
    public static async Task CriarEsquemaDeTesteAsync(this DatabaseFacade baseDeDados)
    {
        if (EmSqlServer) await baseDeDados.MigrateAsync();
        else await baseDeDados.EnsureCreatedAsync();
    }

    private static string CadeiaDe(SqliteConnection ligacao)
    {
        lock (CriacaoDeBases)
        {
            if (Bases.TryGetValue(ligacao, out var existente)) return existente;

            var nome = $"CampusCarpool_Suite_{Guid.NewGuid():N}";
            ExecutarNoServidor($"CREATE DATABASE [{nome}]");

            var cadeia = new SqlConnectionStringBuilder(Integration.SqlServerFactAttribute.ObterConnectionString())
            {
                InitialCatalog = nome
            }.ConnectionString;

            Bases.Add(ligacao, cadeia);
            ligacao.Disposed += (_, _) => Apagar(nome, cadeia);

            return cadeia;
        }
    }

    private static void Apagar(string nome, string cadeia)
    {
        try
        {
            using (var ligacao = new SqlConnection(cadeia)) SqlConnection.ClearPool(ligacao);

            lock (CriacaoDeBases)
                ExecutarNoServidor($"ALTER DATABASE [{nome}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nome}]");
        }
        catch (SqlException)
        {
            // A limpeza não pode fazer falhar o teste; a BD desaparece com o contentor.
        }
    }

    private static void ExecutarNoServidor(string sql)
    {
        using var ligacao = new SqlConnection(Integration.SqlServerFactAttribute.ObterConnectionString());
        ligacao.Open();
        using var comando = ligacao.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        comando.ExecuteNonQuery();
    }
}

/// <summary>Teste que usa SQL próprio do SQLite; fica saltado quando a suite corre em SQL Server.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SoSqliteFactAttribute : FactAttribute
{
    public SoSqliteFactAttribute()
    {
        if (BaseDeDadosDeTeste.EmSqlServer) Skip = "Usa SQL próprio do SQLite.";
    }
}
