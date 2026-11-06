using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Identidade;

/// <summary>Registo, confirmação de conta, início e fim de sessão (F001).</summary>
public interface IAutenticacaoService
{
    /// <summary>Cria uma conta por confirmar e envia a ligação de confirmação (RF-F001-02 a RF-F001-06).</summary>
    /// <returns>
    /// Sucesso; erro 400 com dados inválidos ou email não académico (RF-F001-03); 409 com email já registado
    /// (RF-F001-04); 503 se a conta ficou criada mas o envio do email falhou.
    /// </returns>
    Task<Resultado> RegistarAsync(string nome, string email, string palavraPasse, CancellationToken ct);

    /// <summary>Envia uma nova ligação de confirmação a uma conta por confirmar (RF-F001-21).</summary>
    /// <remarks>
    /// Não revela se o endereço existe na plataforma: dá sucesso exista ou não a conta, e uma falha no envio também
    /// fica escondida. No máximo um reenvio por minuto. As ligações anteriores continuam válidas até expirarem (API-12).
    /// </remarks>
    /// <returns>Sucesso, ou erro 400 com email vazio ou longo.</returns>
    Task<Resultado> ReenviarConfirmacaoAsync(string email, CancellationToken ct);

    /// <summary>Confirma a conta com o token da ligação do email (RF-F001-07, RF-F001-08).</summary>
    /// <remarks>
    /// O token é procurado pelo hash e só serve se não foi usado nem expirou (24 h, P-02). Expirado e já usado dão a
    /// mesma resposta; RF-F001-08 pede que o utilizador saiba o motivo (DEF-BE-003, fica para m4).
    /// </remarks>
    /// <returns>Sucesso; erro 400 com token inválido, expirado ou já usado; 404 se o utilizador não existir.</returns>
    Task<Resultado> ConfirmarContaAsync(string token, CancellationToken ct);

    /// <summary>Inicia sessão e devolve um token de acesso (RF-F001-10, RF-F001-11, P-09, P-17).</summary>
    /// <returns>
    /// O token; erro 401 com credenciais inválidas, com a mesma mensagem para email inexistente e palavra-passe errada
    /// (RF-F001-11); 403 com a conta por confirmar (P-09).
    /// </returns>
    Task<Resultado<TokenAcesso>> IniciarSessaoAsync(string email, string palavraPasse, CancellationToken ct);

    /// <summary>
    /// Termina a sessão: revoga o token atual, que deixa de ser aceite até expirar (RF-F001-12, DEF-BE-004), e
    /// desativa o dispositivo indicado, sem afetar os outros (RF-F006-18, D-020).
    /// </summary>
    /// <param name="jti">Identificador do token.</param>
    /// <param name="expiraEm">Expiração do token; sem ela, guarda-se pela duração máxima (60 min).</param>
    /// <param name="utilizadorId">Utilizador autenticado, se o token o indicar.</param>
    /// <param name="tokenDispositivo">Token FCM enviado pela aplicação Android; o web não envia nada.</param>
    /// <param name="ct">Cancelamento.</param>
    Task TerminarSessaoAsync(
        string jti,
        DateTime? expiraEm,
        Guid? utilizadorId,
        string? tokenDispositivo,
        CancellationToken ct);
}
