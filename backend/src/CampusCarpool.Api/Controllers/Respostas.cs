namespace CampusCarpool.Api.Controllers;

/// <summary>Corpo de uma resposta que é só um texto para o utilizador.</summary>
/// <param name="Mensagem">Texto.</param>
public sealed record RespostaMensagem(string Mensagem);

/// <summary>Corpo de <c>GET /autenticacao/me</c>.</summary>
/// <param name="Id">Id do utilizador autenticado, tal como vem no token.</param>
public sealed record SessaoAtual(string? Id);

/// <summary>Corpo de <c>POST /autenticacao/bilhete-hub</c> (DEF-BE-055).</summary>
/// <param name="Bilhete">Valor a pôr em <c>/hubs/pedidos?bilhete=</c>; serve uma só vez.</param>
/// <param name="ExpiraEm">Fim da validade do bilhete (UTC, 30 s depois de emitido).</param>
public sealed record BilheteHub(string Bilhete, DateTime ExpiraEm);

/// <summary>Corpo de <c>PUT /notificacoes/ler-todas</c>.</summary>
/// <param name="Marcadas">Número de notificações marcadas como lidas.</param>
public sealed record NotificacoesMarcadas(int Marcadas);

/// <summary>Corpo das respostas de erro previstas.</summary>
/// <param name="Erro">Motivo, para o utilizador.</param>
public sealed record CorpoErro(string Erro);
