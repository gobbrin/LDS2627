namespace CampusCarpool.Application.Identidade;

/// <summary>Resposta do início de sessão: o JWT e a validade em segundos (RF-F001-11, P-09).</summary>
/// <remarks>DTO próprio, para a resposta não depender do tipo da integração de tokens (BE-06).</remarks>
public sealed record SessaoIniciada(string AccessToken, int ExpiresIn);

/// <summary>Ligação a um perfil externo, tal como aparece nos perfis.</summary>
public sealed record LigacaoPerfil(Guid Id, string Url, string? Descricao);

/// <summary>Perfil do próprio utilizador, com o email e as ligações externas (RF-F001-20).</summary>
public sealed record PerfilProprio(
    Guid Id,
    string Nome,
    string Email,
    bool EmailConfirmado,
    string? Curso,
    string? Instituicao,
    DateTime CriadoEm,
    IReadOnlyList<LigacaoPerfil> LigacoesExternas,
    decimal? ClassificacaoMedia,
    int TotalAvaliacoes);

/// <summary>Perfil do utilizador depois de alterado.</summary>
public sealed record PerfilAtualizado(
    Guid Id,
    string Nome,
    string Email,
    bool EmailConfirmado,
    string? Curso,
    string? Instituicao);

/// <summary>Perfil que qualquer utilizador pode consultar: só os campos de P-10, nunca o email.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Curso">Curso.</param>
/// <param name="Instituicao">Instituição.</param>
/// <param name="LigacoesExternas">Ligações a perfis externos.</param>
/// <param name="ClassificacaoMedia">Média com 1 casa decimal; <c>null</c> com 0 avaliações, "sem avaliações".</param>
/// <param name="TotalAvaliacoes">Número de avaliações recebidas.</param>
public sealed record PerfilPublico(
    Guid Id,
    string Nome,
    string? Curso,
    string? Instituicao,
    IReadOnlyList<LigacaoPerfil> LigacoesExternas,
    decimal? ClassificacaoMedia,
    int TotalAvaliacoes);
