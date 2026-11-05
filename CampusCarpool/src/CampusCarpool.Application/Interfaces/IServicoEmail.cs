namespace CampusCarpool.Application.Interfaces;

/// <summary>
/// Contrato para envio de mensagens de email sem acoplar a API a um fornecedor.
/// </summary>
/// <remarks>
/// O controller usa esta interface; a implementação Brevo está em
/// <c>Infrastructure/Integrations/ServicoEmail.cs</c>. Um fornecedor diferente
/// pode substituir essa implementação através da injeção de dependências.
/// </remarks>
public interface IServicoEmail
{
    /// <summary>Envia a ligação de confirmação de conta (RF-F001-06).</summary>
    /// <param name="destino">Email do utilizador registado.</param>
    /// <param name="token">Token em claro, acrescentado ao URL <c>Frontend:ConfirmUrl</c>.</param>
    /// <param name="ct">Cancelamento.</param>
    Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct);
}
