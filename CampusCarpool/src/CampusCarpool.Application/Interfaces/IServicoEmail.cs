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
	Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct);
}
