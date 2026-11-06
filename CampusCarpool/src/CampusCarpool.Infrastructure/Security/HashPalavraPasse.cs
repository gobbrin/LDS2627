using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CampusCarpool.Infrastructure.Security;

/// <summary>Palavras-passe com o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core Identity (PBKDF2).</summary>
public sealed class HashPalavraPasse : IHashPalavraPasse
{
    private static readonly PasswordHasher<Utilizador> Hasher = new();

    /// <inheritdoc />
    public string Gerar(Utilizador utilizador, string palavraPasse) => Hasher.HashPassword(utilizador, palavraPasse);

    /// <inheritdoc />
    public bool Verificar(Utilizador utilizador, string palavraPasse)
    {
        ArgumentNullException.ThrowIfNull(utilizador);

        return Hasher.VerifyHashedPassword(utilizador, utilizador.PasswordHash, palavraPasse)
            != PasswordVerificationResult.Failed;
    }
}
