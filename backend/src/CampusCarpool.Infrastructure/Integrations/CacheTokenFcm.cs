namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>Guarda o token OAuth do FCM entre envios (IN-05).</summary>
/// <remarks>
/// O token vale cerca de uma hora; sem isto, cada notificação faria dois pedidos à Google. É um singleton porque o
/// <see cref="ServicoPush"/> é criado de novo para cada notificação.
/// </remarks>
public sealed class CacheTokenFcm
{
    private static readonly TimeSpan Margem = TimeSpan.FromMinutes(5);

    private readonly Lock _bloqueio = new();
    private string? _token;
    private DateTime _renovarEm;

    /// <summary>Token ainda válido; <c>null</c> se não houver ou estiver perto de expirar.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    public string? Obter(DateTime agora)
    {
        lock (_bloqueio)
        {
            return agora < _renovarEm ? _token : null;
        }
    }

    /// <summary>Guarda um token novo.</summary>
    /// <param name="token">Token OAuth.</param>
    /// <param name="validade">Validade indicada pela Google.</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    public void Guardar(string token, TimeSpan validade, DateTime agora)
    {
        lock (_bloqueio)
        {
            _token = token;
            _renovarEm = agora + validade - Margem;
        }
    }

    /// <summary>Esquece o token, para o próximo envio pedir outro.</summary>
    public void Limpar()
    {
        lock (_bloqueio)
        {
            _token = null;
        }
    }
}
