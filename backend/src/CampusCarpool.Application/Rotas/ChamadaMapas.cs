using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Rotas;

/// <summary>Traduz as falhas do Serviço de Mapas em erros previstos (RF-F008-12).</summary>
internal static class ChamadaMapas
{
    /// <summary>O serviço não tem chave configurada (503).</summary>
    public static readonly Erro NaoConfigurado = new(TipoErro.ServicoIndisponivel, "Serviço de mapas não configurado.");

    /// <summary>Executa uma operação que chama o Serviço de Mapas.</summary>
    /// <typeparam name="T">Tipo do valor devolvido.</typeparam>
    /// <param name="operacao">Operação.</param>
    /// <param name="mensagemFalha">Texto do erro 502, se o serviço responder com erro.</param>
    /// <param name="mensagemTempo">Texto do erro 504, se o serviço não responder a tempo.</param>
    /// <param name="ct">Cancelamento do pedido; se for ele a cancelar, a exceção segue, porque o cliente já saiu.</param>
    /// <returns>O resultado da operação ou o erro correspondente à falha.</returns>
    public static async Task<Resultado<T>> ExecutarAsync<T>(
        Func<Task<Resultado<T>>> operacao,
        string mensagemFalha,
        string mensagemTempo,
        CancellationToken ct)
    {
        try
        {
            return await operacao();
        }
        catch (HttpRequestException)
        {
            return new Erro(TipoErro.FalhaServicoExterno, mensagemFalha);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new Erro(TipoErro.TempoEsgotado, mensagemTempo);
        }
    }
}
