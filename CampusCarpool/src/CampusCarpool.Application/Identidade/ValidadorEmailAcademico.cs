using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CampusCarpool.Application.Identidade;

/// <summary>Configuração dos domínios de email aceites (secção <c>EmailAcademico</c>, P-01).</summary>
public sealed class OpcoesEmailAcademico
{
    /// <summary>Nome da secção na configuração.</summary>
    public const string Seccao = "EmailAcademico";

    /// <summary>Domínios aceites, como <c>estg.ipp.pt</c>. Sem nenhum, todos os registos são recusados.</summary>
    public IList<string> Dominios { get; } = new List<string>();
}

/// <summary>Verifica se um email pertence a um domínio académico da lista configurada (P-01, RF-F001-03).</summary>
public sealed class ValidadorEmailAcademico(IOptions<OpcoesEmailAcademico> opcoes)
{
    /// <summary>Indica se o email tem formato válido e termina num dos domínios configurados, sem distinguir maiúsculas.</summary>
    public bool Permitido(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return FormatoValido(email)
            && opcoes.Value.Dominios.Any(d => email.EndsWith($"@{d.TrimStart('@')}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Um só <c>@</c>, com parte local, sem espaços e aceite por <see cref="MailAddress"/>.</summary>
    private static bool FormatoValido(string email)
    {
        var arroba = email.IndexOf('@', StringComparison.Ordinal);

        return arroba > 0
            && arroba == email.LastIndexOf('@')
            && !email.Any(char.IsWhiteSpace)
            && MailAddress.TryCreate(email, out var endereco)
            && endereco.Address == email;
    }
}
