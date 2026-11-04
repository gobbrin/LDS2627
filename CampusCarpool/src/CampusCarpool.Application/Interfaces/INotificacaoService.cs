
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Serviço responsável pela criação e gestão de notificações.
/// </summary>
/// <remarks>
/// Quem chama decide os destinatários; as regras de P-23 (quem recebe "Alteração da viagem") estão espalhadas
/// por <c>ParticipacaoService</c>, <c>ViagensController</c> e <c>PedidosController</c> (AP-03, DC-38).
/// </remarks>
public interface INotificacaoService
{
    /// <summary>
    /// Regista uma notificação para o utilizador destinatário.
    /// </summary>
    /// <remarks>
    /// Grava logo a notificação e, só para <c>PedidoRecebido</c>, <c>PedidoAceite</c> e <c>PedidoRejeitado</c>,
    /// coloca um push na fila (RF-F006-11; sem push para os restantes tipos, RF-F006-12/16/17).
    /// </remarks>
    /// <param name="utilizadorId">Destinatário.</param>
    /// <param name="tipo">Tipo; o título é o nome do tipo (DC-40).</param>
    /// <param name="mensagem">Texto da notificação.</param>
    /// <param name="ct">Cancelamento.</param>
    Task<Notificacao> CriarAsync(
        Guid utilizadorId,
        TipoNotificacao tipo,
        string mensagem,
        CancellationToken ct);

    /// <summary>
    /// Devolve uma página de notificações pessoais,
    /// da mais recente para a mais antiga.
    /// </summary>
    /// <remarks>RF-F006-07.</remarks>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="pagina">Número da página, a partir de 1.</param>
    /// <param name="tamanhoPagina">Itens por página, de 1 a 100.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <exception cref="ArgumentOutOfRangeException">Página ou tamanho fora dos limites.</exception>
    Task<PaginaNotificacoes> ListarAsync(
        Guid utilizadorId,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct);

    /// <summary>
    /// Marca uma notificação pessoal como lida.
    /// </summary>
    /// <returns><c>false</c> se a notificação não existe ou é de outro utilizador; <c>true</c> caso contrário (também se já estava lida).</returns>
    Task<bool> MarcarComoLidaAsync(
        Guid notificacaoId,
        Guid utilizadorId,
        CancellationToken ct);

    /// <summary>
    /// Marca todas as notificações pessoais não lidas como lidas.
    /// </summary>
    /// <returns>Número de notificações marcadas.</returns>
    Task<int> MarcarTodasComoLidasAsync(
        Guid utilizadorId,
        CancellationToken ct);
}

/// <summary>
/// Resultado paginado para a caixa de notificações.
/// </summary>
/// <param name="Itens">Notificações da página pedida.</param>
/// <param name="Pagina">Página devolvida.</param>
/// <param name="TamanhoPagina">Tamanho da página.</param>
/// <param name="Total">Total de notificações do utilizador.</param>
/// <param name="TotalPaginas">Total de páginas.</param>
/// <param name="NaoLidas">Total de notificações por ler (para o contador da interface).</param>
public sealed record PaginaNotificacoes(
    IReadOnlyList<Notificacao> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total,
    int TotalPaginas,
    int NaoLidas);
