using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>
/// Cliente da HERE como Serviço de Mapas principal (D-5): o que envia e como lê a geocodificação, a rota direta e a
/// matriz. A HERE é a <see cref="HereMapasFalsa"/> (HttpMessageHandler falso); nenhum pedido sai da máquina.
/// </summary>
public sealed class ServicoMapasHereTests : IDisposable
{
    private readonly HereMapasFalsa here = new();

    public void Dispose() => here.Dispose();

    [Fact]
    /// <summary>Geocodificação: envia o texto, o filtro de Portugal e a chave; lê a posição do primeiro resultado.</summary>
    public async Task I112_62_Geocodificar_LeAPosicaoDoPrimeiroResultado()
    {
        here.Locais["Felgueiras"] = (41.3647, -8.1977);

        Coordenadas? coordenadas = await here.CriarServico().GeocodificarAsync("Felgueiras", default);

        Assert.Equal(new Coordenadas(41.3647m, -8.1977m), coordenadas);
        string pedido = Uri.UnescapeDataString(here.UltimoPedido!.ToString());
        Assert.StartsWith("https://geocode.search.hereapi.com/v1/geocode?", pedido, StringComparison.Ordinal);
        Assert.Contains("q=Felgueiras", pedido, StringComparison.Ordinal);
        Assert.Contains("in=countryCode:PRT", pedido, StringComparison.Ordinal);
        Assert.Contains("apiKey=chave-here-ficticia-apenas-para-testes", pedido, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>Um local que a HERE não encontra (lista vazia) dá <c>null</c>, não erro.</summary>
    public async Task I112_63_Geocodificar_SemResultados_DevolveNull()
    {
        Assert.Null(await here.CriarServico().GeocodificarAsync("Sítio que não existe", default));
        Assert.Equal(1, here.ChamadasGeocodificacao);
    }

    [Fact]
    /// <summary>Rota direta: lê a distância (m → km), a duração (s → min, para cima) e a linha, no mesmo pedido.</summary>
    public async Task I112_64_RotaDireta_TrazDistanciaDuracaoELinha()
    {
        ResultadoRota? rota = await here.CriarServico().CalcularAsync(
            new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m), default);

        Assert.NotNull(rota);
        Assert.Equal(12.3m, rota.DistanciaKm);
        Assert.Equal(15, rota.DuracaoMinutos);
        Assert.Equal(4, rota.Geometria!.Count);
        Assert.Equal(new Coordenadas(50.10228m, 8.69821m), rota.Geometria[0]);
        string pedido = here.UltimoPedido!.ToString();
        Assert.Contains("origin=41.15,-8.61", pedido, StringComparison.Ordinal);
        Assert.Contains("destination=41.55,-8.42", pedido, StringComparison.Ordinal);
        Assert.Contains("return=summary,polyline", pedido, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>
    /// Matriz: um só POST com os pontos como origens e destinos e a região <c>autoCircle</c>; a lista plana lê-se na
    /// posição n × i + j, e um par com <c>errorCodes</c> diferente de 0 fica sem rota (<c>null</c>).
    /// </summary>
    public async Task I112_65_Matriz_LeAListaPlanaEOsParesSemRota()
    {
        here.ParesSemRota.Add((0, 2));
        Coordenadas[] pontos = [new(41.15m, -8.61m), new(41.23m, -8.62m), new(41.55m, -8.42m)];

        MatrizRotas? matriz = await here.CriarServico().CalcularMatrizAsync(pontos, default);

        Assert.NotNull(matriz);
        Assert.Equal(1, here.ChamadasMatriz);
        Assert.Equal(120d, matriz.DuracoesSegundos[0][1]);
        Assert.Equal(1000d, matriz.DistanciasMetros[1][2]);
        Assert.Equal(0d, matriz.DuracoesSegundos[2][2]);
        Assert.Null(matriz.DuracoesSegundos[0][2]);
        Assert.Null(matriz.DistanciasMetros[0][2]);

        using JsonDocument corpo = JsonDocument.Parse(here.UltimoCorpo!);
        Assert.Equal(3, corpo.RootElement.GetProperty("origins").GetArrayLength());
        Assert.Equal(3, corpo.RootElement.GetProperty("destinations").GetArrayLength());
        Assert.Equal(41.23m, corpo.RootElement.GetProperty("origins")[1].GetProperty("lat").GetDecimal());
        Assert.Equal("autoCircle", corpo.RootElement.GetProperty("regionDefinition").GetProperty("type").GetString());
        Assert.Contains("async=false", here.UltimoPedido!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>Um erro da HERE sai como <see cref="HttpRequestException"/>, para o serviço com reserva passar ao OpenRouteService.</summary>
    public async Task I112_66_ErroDaHere_SaiComoHttpRequestException()
    {
        here.Falha = System.Net.HttpStatusCode.TooManyRequests;

        await Assert.ThrowsAsync<HttpRequestException>(() => here.CriarServico().GeocodificarAsync("Porto", default));
    }

    [Fact]
    /// <summary>
    /// DEF-BE-048: uma duração fora do intervalo de um <c>int</c> não pode dar 500: sai como <see cref="HttpRequestException"/>
    /// (erro do Serviço de Mapas, 502) e o serviço com reserva passa ao OpenRouteService.
    /// </summary>
    public async Task I112_124_DuracaoForaDoIntervalo_SaiComoHttpRequestException()
    {
        here.RespostaRota = "{\"routes\":[{\"sections\":[{\"summary\":{\"length\":1000,\"duration\":1e30},\"polyline\":\"" + HereFalsa.Polilinha + "\"}]}]}";

        await Assert.ThrowsAsync<HttpRequestException>(() => here.CriarServico().CalcularAsync(
            new Coordenadas(41.15m, -8.61m), new Coordenadas(41.55m, -8.42m), default));
    }

    [Fact]
    /// <summary>
    /// Sem caminho de estrada entre os pontos (ex.: um ponto numa ilha), a HERE responde 200 com <c>routes</c> vazio:
    /// a rota é <c>null</c> («sem rota»), não um erro, e o serviço com reserva não chama o OpenRouteService por isso.
    /// </summary>
    public async Task I112_105_RotaDireta_SemCaminho_DevolveNull()
    {
        here.RespostaRota = "{\"routes\":[]}";

        ResultadoRota? rota = await here.CriarServico().CalcularAsync(
            new Coordenadas(41.15m, -8.61m), new Coordenadas(38.72m, -27.22m), default);

        Assert.Null(rota);
        Assert.Equal(1, here.ChamadasRota);
    }

    [Fact]
    /// <summary>
    /// Sem <c>Here:ApiKey</c> nenhuma operação sai para a rede: lançam <see cref="InvalidOperationException"/>, que o
    /// serviço com reserva trata como «HERE indisponível» e passa ao OpenRouteService.
    /// </summary>
    public async Task I112_106_SemChave_NaoFazPedidosALaHere()
    {
        var servico = here.CriarServico(configurado: false);
        Coordenadas porto = new(41.15m, -8.61m);
        Coordenadas braga = new(41.55m, -8.42m);

        Assert.False(servico.Configurado);
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GeocodificarAsync("Porto", default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.CalcularAsync(porto, braga, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.CalcularMatrizAsync([porto, braga], default));
        Assert.Null(here.UltimoPedido);
    }

    [Fact]
    /// <summary>Uma matriz precisa de pelo menos dois pontos; com um só, o erro é do programador e não vai à HERE.</summary>
    public async Task I112_107_Matriz_ComUmSoPonto_RecusaSemPedido()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => here.CriarServico().CalcularMatrizAsync([new Coordenadas(41.15m, -8.61m)], default));

        Assert.Equal(0, here.ChamadasMatriz);
    }
}
