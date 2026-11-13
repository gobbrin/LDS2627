using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Acesso a dados das ligações a perfis externos.</summary>
/// <remarks>Adicionar e remover só ficam gravados com a unidade de trabalho.</remarks>
public interface IRepositorioLigacoesExternas
{
    /// <summary>Lista as ligações do utilizador, pela ordem do perfil (só leitura).</summary>
    Task<IReadOnlyList<LigacaoExterna>> ListarAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Obtém uma ligação do utilizador, para a alterar; <c>null</c> se não existir ou for de outro.</summary>
    Task<LigacaoExterna?> ObterAsync(Guid ligacaoId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Acrescenta uma ligação.</summary>
    void Adicionar(LigacaoExterna ligacao);

    /// <summary>Remove uma ligação obtida com <see cref="ObterAsync"/>.</summary>
    void Remover(LigacaoExterna ligacao);
}
