using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Notificacoes;

/// <summary>Notificação na caixa do utilizador.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="UtilizadorId">Destinatário.</param>
/// <param name="ViagemId">Viagem relacionada, se existir.</param>
/// <param name="Tipo">Nome do <see cref="TipoNotificacao"/>.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Conteudo">Texto.</param>
/// <param name="LidaEm">Data de leitura, ou <c>null</c>.</param>
/// <param name="CriadaEm">Data de criação.</param>
public sealed record NotificacaoPessoal(
    Guid Id,
    Guid UtilizadorId,
    Guid? ViagemId,
    string Tipo,
    string Titulo,
    string Conteudo,
    DateTime? LidaEm,
    DateTime CriadaEm);

/// <summary>Página da caixa de notificações.</summary>
/// <param name="Itens">Notificações da página pedida.</param>
/// <param name="Pagina">Página devolvida.</param>
/// <param name="TamanhoPagina">Tamanho da página.</param>
/// <param name="Total">Total de notificações do utilizador.</param>
/// <param name="TotalPaginas">Total de páginas.</param>
/// <param name="NaoLidas">Total de notificações por ler (para o contador da interface).</param>
public sealed record PaginaNotificacoes(
    IReadOnlyList<NotificacaoPessoal> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total,
    int TotalPaginas,
    int NaoLidas);
