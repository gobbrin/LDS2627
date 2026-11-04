using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Acesso a dados das notificações pessoais.</summary>
/// <remarks>
/// Implementado por <c>Infrastructure/Repositories/RepositorioNotificacoes.cs</c>. O <c>AdicionarAsync</c>
/// grava logo, por isso cada notificação fica numa gravação separada da operação que a originou (AP-02, DC-37).
/// </remarks>
public interface IRepositorioNotificacoes
{

    /// <summary>Guarda uma nova notificação na base de dados.</summary>
    Task AdicionarAsync(Notificacao notificacao, CancellationToken ct);

    /// <summary>Consulta uma página das notificações de um utilizador, ordenadas da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<Notificacao>> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct);

    /// <summary>Obtém uma notificação pertencente ao utilizador indicado (rastreada, para ser alterada); <c>null</c> se for de outro.</summary>
    Task<Notificacao?> ObterAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Obtém as notificações ainda não lidas de um utilizador (rastreadas).</summary>
    Task<IReadOnlyList<Notificacao>> ListarNaoLidasAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Conta as notificações de um utilizador.</summary>
    Task<int> ContarAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Conta as notificações não lidas de um utilizador.</summary>
    Task<int> ContarNaoLidasAsync(Guid utilizadorId, CancellationToken ct);

    /// <summary>Guarda as alterações efetuadas às notificações.</summary>
    Task GuardarAlteracoesAsync(CancellationToken ct);
}
