using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Integrations;

/// <summary>Protege as palavras-passe; a base de dados só guarda o resultado.</summary>
/// <remarks>Implementado por <c>Infrastructure/Security/HashPalavraPasse.cs</c>.</remarks>
public interface IHashPalavraPasse
{
    /// <summary>Calcula o hash da palavra-passe.</summary>
    string Gerar(Utilizador utilizador, string palavraPasse);

    /// <summary>Indica se a palavra-passe corresponde ao hash guardado no utilizador.</summary>
    bool Verificar(Utilizador utilizador, string palavraPasse);
}
