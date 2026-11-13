using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Identidade;

/// <summary>Registo dos dispositivos Android para push (RF-F006-10).</summary>
public interface IDispositivoService
{
    /// <summary>Regista (ou reativa) o dispositivo para o utilizador.</summary>
    /// <returns>Sucesso, ou erro 400 com o token vazio ou longo.</returns>
    Task<Resultado> RegistarAsync(Guid utilizadorId, string token, CancellationToken ct);

    /// <summary>Desativa o dispositivo, que deixa de receber push.</summary>
    /// <returns>Sucesso, mesmo que o token não exista, ou erro 400 com o token vazio.</returns>
    Task<Resultado> DesativarAsync(Guid utilizadorId, string token, CancellationToken ct);
}
