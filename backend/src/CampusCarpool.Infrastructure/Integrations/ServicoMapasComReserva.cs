using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>
/// O Serviço de Mapas que a aplicação usa (D-5): a HERE como principal e o OpenRouteService como reserva, mais a
/// memória das geocodificações (D-3).
/// </summary>
/// <remarks>
/// <para>Ordem, em cada operação (geocodificar, rota direta, matriz):</para>
/// <list type="number">
/// <item>Sem <c>Here:ApiKey</c>: só o OpenRouteService (como antes da I112).</item>
/// <item>Com HERE: pede à HERE. Se ela falhar (erro HTTP, incluindo 401, 403, 429 e 5xx, resposta sem o formato
/// esperado, ou o seu limite de tempo de 5 s) e o OpenRouteService estiver configurado, pede ao OpenRouteService e
/// fica um aviso no log.</item>
/// <item>Se o limite de 10 s da operação inteira (P-19, <c>ChamadaMapas</c>) já acabou, não há reserva: a exceção sobe e
/// a Application responde 504. Se a reserva também falhar, a exceção dela sobe (502 ou 504), como antes.</item>
/// </list>
/// <para>Um resultado «não encontrado» (<c>null</c>) não é uma falha: não se pergunta à reserva.</para>
/// <para>Memória das geocodificações (BE-38 a, D-3): o texto normalizado (sem espaços nas pontas, sem distinguir
/// maiúsculas, a regra de <c>RegraMesmoLocal</c>) dá as coordenadas durante <see cref="ValidadeDaGeocodificacao"/>.
/// Assim, pré-visualizar (<c>POST /rotas/calcular</c>) e criar a viagem logo a seguir não geocodifica duas vezes. Só se
/// guardam locais encontrados. A rota não se guarda aqui.</para>
/// </remarks>
public sealed class ServicoMapasComReserva(
    ServicoMapasHere here,
    ServicoMapasRotas openRouteService,
    IMemoryCache memoria,
    ILogger<ServicoMapasComReserva> logger) : IServicoMapasRotas
{
    /// <summary>Tempo durante o qual uma geocodificação é reutilizada.</summary>
    public static readonly TimeSpan ValidadeDaGeocodificacao = TimeSpan.FromMinutes(10);

    private static readonly Action<ILogger, string, Exception?> LogReserva = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(8102, nameof(LogReserva)),
        "HERE falhou ({Operacao}); a usar o OpenRouteService (reserva).");

    // PM-1: sem reserva configurada, uma falha da HERE fica visível no log (só o código HTTP; nunca a chave).
    private static readonly Action<ILogger, string, string, Exception?> LogSemReserva = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(8103, nameof(LogSemReserva)),
        "HERE falhou ({Operacao}, estado HTTP {Estado}) e não há reserva configurada.");

    /// <summary>Indica se há pelo menos um fornecedor configurado; sem nenhum, a Application responde 503.</summary>
    public bool Configurado => here.Configurado || openRouteService.Configurado;

    /// <inheritdoc />
    public async Task<Coordenadas?> GeocodificarAsync(string texto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(texto);

        string chave = "geocodificacao|" + texto.Trim().ToUpperInvariant();

        if (memoria.TryGetValue(chave, out Coordenadas? guardadas))
            return guardadas;

        Coordenadas? coordenadas = await ComReservaAsync(
            "geocodificação",
            limite => here.GeocodificarAsync(texto, limite),
            limite => openRouteService.GeocodificarAsync(texto, limite),
            ct);

        // Um local não encontrado não se guarda: pode ser um erro de escrita que o utilizador corrige já a seguir.
        if (coordenadas is not null)
        {
            var opcoes = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ValidadeDaGeocodificacao, Size = 1 };
            memoria.Set(chave, coordenadas, opcoes);
        }

        return coordenadas;
    }

    /// <inheritdoc />
    public Task<ResultadoRota?> CalcularAsync(Coordenadas origem, Coordenadas destino, CancellationToken ct) =>
        ComReservaAsync(
            "rota direta",
            limite => here.CalcularAsync(origem, destino, limite),
            limite => openRouteService.CalcularAsync(origem, destino, limite),
            ct);

    /// <inheritdoc />
    public Task<MatrizRotas?> CalcularMatrizAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct) =>
        ComReservaAsync(
            "matriz",
            limite => here.CalcularMatrizAsync(pontos, limite),
            limite => openRouteService.CalcularMatrizAsync(pontos, limite),
            ct);

    /// <summary>Faz a operação na HERE e, se ela falhar, no OpenRouteService.</summary>
    /// <typeparam name="T">Tipo do resultado.</typeparam>
    /// <param name="operacao">Nome da operação, para o aviso no log.</param>
    /// <param name="principal">Operação na HERE.</param>
    /// <param name="reserva">A mesma operação no OpenRouteService.</param>
    /// <param name="ct">Limite de 10 s da operação (P-19), ou o cancelamento do pedido.</param>
    /// <returns>O resultado da HERE ou, se ela falhou, o da reserva.</returns>
    private async Task<T> ComReservaAsync<T>(
        string operacao,
        Func<CancellationToken, Task<T>> principal,
        Func<CancellationToken, Task<T>> reserva,
        CancellationToken ct)
    {
        if (!here.Configurado)
            return await reserva(ct);

        try
        {
            return await principal(ct);
        }
        catch (HttpRequestException ex) when (openRouteService.Configurado)
        {
            LogReserva(logger, operacao, ex);
        }
        catch (TaskCanceledException ex) when (openRouteService.Configurado && !ct.IsCancellationRequested)
        {
            // Esgotou o tempo do HttpClient da HERE (5 s), não o limite da operação: ainda há tempo para a reserva.
            LogReserva(logger, operacao, ex);
        }
        catch (HttpRequestException ex)
        {
            // Sem reserva (a condição anterior não se verificou): regista a causa e deixa o erro seguir (502).
            LogSemReserva(logger, operacao, ex.StatusCode?.ToString() ?? "sem resposta", ex);
            throw;
        }

        return await reserva(ct);
    }
}
