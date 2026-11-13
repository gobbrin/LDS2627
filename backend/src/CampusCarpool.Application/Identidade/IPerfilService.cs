using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Identidade;

/// <summary>Perfil do utilizador autenticado e perfil público de outros utilizadores (F001).</summary>
public interface IPerfilService
{
    /// <summary>Perfil do próprio utilizador (RF-F001-20).</summary>
    /// <returns>O perfil, ou erro 404 se o utilizador não existir.</returns>
    Task<Resultado<PerfilProprio>> ConsultarAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Perfil público de um utilizador (RF-F001-17, RF-F001-19, RF-F007-10; DEF-BE-006).</summary>
    /// <returns>O perfil, ou erro 404 se o utilizador não existir.</returns>
    Task<Resultado<PerfilPublico>> ConsultarPublicoAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Altera nome, curso e instituição do próprio utilizador (RF-F001-14).</summary>
    /// <remarks>
    /// Tamanhos máximos de P-26: nome 100, curso e instituição 150 caracteres. Curso e instituição vazios passam a
    /// <c>null</c>.
    /// </remarks>
    /// <returns>O perfil atualizado; erro 400 com dados inválidos; 404 se o utilizador não existir.</returns>
    Task<Resultado<PerfilAtualizado>> AtualizarAsync(
        Guid utilizadorId,
        string nome,
        string? curso,
        string? instituicao,
        CancellationToken ct);

    /// <summary>Associa uma ligação a um perfil externo ao próprio perfil (RF-F001-15, RF-F001-16, P-27, DEF-BE-005).</summary>
    /// <returns>A ligação; erro 400 se o URL não for absoluto http ou https, o texto for longo ou houver mais de 5 ligações.</returns>
    Task<Resultado<LigacaoPerfil>> AdicionarLigacaoAsync(
        Guid utilizadorId,
        string? url,
        string? descricao,
        CancellationToken ct);

    /// <summary>Remove uma ligação do próprio perfil (RF-F001-22).</summary>
    /// <returns>Sucesso, ou erro 404 se a ligação não existir ou for de outro utilizador.</returns>
    Task<Resultado> RemoverLigacaoAsync(Guid utilizadorId, Guid ligacaoId, CancellationToken ct);
}
