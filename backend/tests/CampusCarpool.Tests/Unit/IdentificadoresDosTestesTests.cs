using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>
/// Rastreabilidade dos testes (R-Q04, BE-40): cada identificador <c>I&lt;issue&gt;_&lt;número&gt;</c> no início do nome de
/// um teste aparece uma só vez, para «I112_05 falhou» apontar para um único teste.
/// </summary>
public sealed partial class IdentificadoresDosTestesTests
{
    // «I112_05_Nome» → «I112_05».
    [GeneratedRegex(@"^(I\d+_\d+)_")]
    private static partial Regex Identificador();

    [Fact]
    /// <summary>Percorre os métodos de teste deste assembly e falha se dois tiverem o mesmo identificador.</summary>
    public void I112_80_IdentificadoresDosTestes_NaoSeRepetem()
    {
        var vistos = new Dictionary<string, string>();
        var repetidos = new List<string>();

        foreach (Type tipo in typeof(IdentificadoresDosTestesTests).Assembly.GetTypes())
        {
            foreach (MethodInfo metodo in tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                // Só os testes do xUnit ([Fact] e [Theory], que herda de [Fact]).
                if (metodo.GetCustomAttribute<FactAttribute>() is null)
                    continue;

                Match encontrado = Identificador().Match(metodo.Name);
                if (!encontrado.Success)
                    continue;

                string id = encontrado.Groups[1].Value;
                string onde = tipo.Name + "." + metodo.Name;

                if (vistos.TryGetValue(id, out string? primeiro))
                    repetidos.Add(id + ": " + primeiro + " e " + onde);
                else
                    vistos[id] = onde;
            }
        }

        // Prova que a procura encontrou os testes (há mais de 200 com identificador).
        Assert.True(vistos.Count > 200, $"Só {vistos.Count} testes com identificador.");
        Assert.Empty(repetidos);
    }
}
