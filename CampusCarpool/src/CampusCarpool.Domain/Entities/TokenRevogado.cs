namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Token JWT revogado pelo fim de sessão (RF-F001-12, DEF-BE-004).
/// </summary>
/// <remarks>
/// O token é identificado pela claim <c>jti</c>. A validação do JwtBearer
/// (<c>ValidacaoTokensRevogados</c>) recusa qualquer token cujo <c>jti</c> esteja nesta tabela.
/// <see cref="ExpiraEm"/> é a expiração do próprio token: depois dela o registo deixa de ser
/// necessário (ainda não há limpeza automática).
/// </remarks>
public sealed class TokenRevogado
{
    /// <summary>Identificador do registo.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Valor da claim <c>jti</c> do token revogado.</summary>
    public string Jti { get; set; } = "";

    /// <summary>Expiração (UTC) do token revogado.</summary>
    public DateTime ExpiraEm { get; set; }
}
