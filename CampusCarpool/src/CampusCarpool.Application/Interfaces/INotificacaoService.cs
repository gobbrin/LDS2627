
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Serviço responsável pela criação e gestão de notificações.
/// </summary>
public interface INotificacaoService
{
    /// <summary>
    /// Regista uma notificação para o utilizador destinatário.
    /// </summary>
    Task<Notificacao> CriarAsync(
        Guid utilizadorId,
        TipoNotificacao tipo,
        string mensagem,
        CancellationToken ct);

    /// <summary>
    /// Devolve uma página de notificações pessoais,
    /// da mais recente para a mais antiga.
    /// </summary>
    Task<PaginaNotificacoes> ListarAsync(
        Guid utilizadorId,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct);

    /// <summary>
    /// Marca uma notificação pessoal como lida.
    /// </summary>
    Task<bool> MarcarComoLidaAsync(
        Guid notificacaoId,
        Guid utilizadorId,
        CancellationToken ct);

    /// <summary>
    /// Marca todas as notificações pessoais não lidas como lidas.
    /// </summary>
    Task<int> MarcarTodasComoLidasAsync(
        Guid utilizadorId,
        CancellationToken ct);
}

/// <summary>
/// Resultado paginado para a caixa de notificações.
/// </summary>
public sealed record PaginaNotificacoes(
    IReadOnlyList<Notificacao> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total,
    int TotalPaginas,
    int NaoLidas);
