using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Notificacoes;

/// <summary>Criação e consulta das notificações pessoais (F006).</summary>
/// <remarks>
/// Os métodos <c>Registar</c> não gravam: juntam a notificação às alterações pendentes, para ficar na mesma gravação
/// da operação que a originou (DC-37, RF-F006-01, RF-F006-02). Quem os chama confirma a unidade de trabalho e, só
/// depois, envia o push com <see cref="EnviarPushAsync"/>, para nunca anunciar uma operação que não ficou gravada.
/// A regra de P-23 (quem recebe "Alteração da viagem") está só em <see cref="RegistarAlteracaoViagem"/> (DC-38).
/// </remarks>
public interface INotificacaoService
{
    /// <summary>Regista uma notificação para o destinatário.</summary>
    /// <param name="utilizadorId">Destinatário.</param>
    /// <param name="tipo">Tipo; define o título apresentado (DC-40).</param>
    /// <param name="mensagem">Texto da notificação.</param>
    /// <returns>A notificação, para o push depois de gravar.</returns>
    Notificacao Registar(Guid utilizadorId, TipoNotificacao tipo, string mensagem, Guid? viagemId = null);

    /// <summary>
    /// Regista a notificação Alteração da viagem para o condutor e os passageiros indicados, exceto quem originou
    /// a alteração (P-23, RF-F006-02 a RF-F006-04, RF-F006-14).
    /// </summary>
    /// <param name="condutorId">Condutor da viagem.</param>
    /// <param name="passageiros">Passageiros afetados: os aceites e, na indisponibilização, também os pendentes.</param>
    /// <param name="origemId">Quem originou a alteração; nunca é notificado.</param>
    /// <param name="mensagem">Texto da notificação; não pode revelar os pontos de outros passageiros (P-13).</param>
    void RegistarAlteracaoViagem(Guid viagemId, Guid condutorId, IEnumerable<Guid> passageiros, Guid origemId, string mensagem);

    /// <summary>Regista uma notificação de aviso para cada passageiro aceite, ligada à viagem (RF-F006-05).</summary>
    /// <param name="viagemId">Viagem do aviso.</param>
    /// <param name="passageiros">Passageiros aceites.</param>
    /// <param name="texto">Texto do aviso, copiado para a notificação.</param>
    void RegistarAviso(Guid viagemId, IEnumerable<Guid> passageiros, string texto);

    /// <summary>
    /// Coloca o push de uma notificação já gravada na fila, se o tipo o previr (RF-F006-11; sem push para os restantes
    /// tipos, RF-F006-12, RF-F006-16, RF-F006-17).
    /// </summary>
    /// <param name="notificacao">Notificação devolvida por <see cref="Registar"/>.</param>
    /// <param name="ct">Cancelamento.</param>
    Task EnviarPushAsync(Notificacao notificacao, CancellationToken ct);

    /// <summary>Devolve uma página de notificações pessoais, da mais recente para a mais antiga (RF-F006-07).</summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="pagina">Número da página, a partir de 1.</param>
    /// <param name="tamanhoPagina">Itens por página, de 1 a 100.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <exception cref="ArgumentOutOfRangeException">Página ou tamanho fora dos limites; o controller já os valida.</exception>
    Task<PaginaNotificacoes> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct);

    /// <summary>Marca uma notificação pessoal como lida; sem efeito se já estiver lida.</summary>
    /// <returns>Sucesso, ou erro 404 se a notificação não existir ou for de outro utilizador.</returns>
    Task<Resultado> MarcarComoLidaAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Marca todas as notificações pessoais não lidas como lidas.</summary>
    /// <returns>Número de notificações marcadas.</returns>
    Task<int> MarcarTodasComoLidasAsync(Guid utilizadorId, CancellationToken ct);
}
