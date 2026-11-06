using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Identidade;

/// <summary>Implementação de <see cref="IDispositivoService"/>.</summary>
public sealed class DispositivoService(IRepositorioDispositivos dispositivos) : IDispositivoService
{
    private const int TamanhoMaximoToken = 4096;

    /// <inheritdoc />
    public async Task<Resultado> RegistarAsync(Guid utilizadorId, string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > TamanhoMaximoToken)
            return new Erro(TipoErro.DadosInvalidos, "Token FCM inválido.");

        await dispositivos.RegistarAsync(utilizadorId, token.Trim(), ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> DesativarAsync(Guid utilizadorId, string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new Erro(TipoErro.DadosInvalidos);

        await dispositivos.DesativarAsync(utilizadorId, token.Trim(), ct);

        return Resultado.Sucesso;
    }
}
