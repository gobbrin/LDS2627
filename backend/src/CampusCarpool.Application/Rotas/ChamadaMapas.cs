using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Rotas;

/// <summary>
/// Único sítio onde se chama o Serviço de Mapas (OpenRouteService): aplica o limite de tempo de P-19 e traduz as
/// falhas em erros previstos (RF-F008-12).
/// </summary>
internal static class ChamadaMapas
{
    /// <summary>
    /// P-19: «se o serviço não responder em 10 segundos, considera-se indisponível». O limite vale para a operação
    /// inteira (por exemplo, duas geocodificações e uma rota), não para cada pedido HTTP (BE-11).
    /// </summary>
    public static readonly TimeSpan Limite = TimeSpan.FromSeconds(10);

    /// <summary>O serviço não tem chave configurada (503).</summary>
    public static readonly Erro NaoConfigurado = new(TipoErro.ServicoIndisponivel, "Serviço de mapas não configurado.");

    /// <summary>Executa uma operação que chama o Serviço de Mapas, com o limite de 10 s para a operação inteira.</summary>
    /// <typeparam name="T">Tipo do valor devolvido.</typeparam>
    /// <param name="operacao">
    /// Operação. Recebe o cancelamento a usar em todas as chamadas aos mapas: é cancelado ao fim de 10 s ou quando o
    /// cliente sai.
    /// </param>
    /// <param name="mensagemFalha">Texto do erro 502, se o serviço responder com erro.</param>
    /// <param name="mensagemTempo">Texto do erro 504, se o serviço não responder a tempo.</param>
    /// <param name="ct">Cancelamento do pedido; se for ele a cancelar, a exceção segue, porque o cliente já saiu.</param>
    /// <returns>O resultado da operação ou o erro correspondente à falha.</returns>
    public static async Task<Resultado<T>> ExecutarAsync<T>(
        Func<CancellationToken, Task<Resultado<T>>> operacao,
        string mensagemFalha,
        string mensagemTempo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operacao);

        // Cancelado pelo cliente (ct) ou ao fim do limite, o que acontecer primeiro.
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(Limite);

        try
        {
            return await operacao(limite.Token);
        }
        catch (HttpRequestException)
        {
            return new Erro(TipoErro.FalhaServicoExterno, mensagemFalha);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Esgotou o limite da operação ou o de um pedido HTTP: o cliente continua à espera, por isso é 504.
            return new Erro(TipoErro.TempoEsgotado, mensagemTempo);
        }
    }
}
