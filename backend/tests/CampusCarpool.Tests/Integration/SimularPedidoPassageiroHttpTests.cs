using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// <c>POST /api/v1/viagens/{id}/simular-pedido</c> (ecrã 14 do protótipo) e a visibilidade dos pontos na rota
/// (P-13, RF-F003-08, RF-F008-09), pelo pipeline HTTP real (<see cref="AplicacaoReal"/>).
/// </summary>
/// <remarks>
/// O Serviço de Mapas é o <see cref="MapasFalsos"/> da aplicação e a HERE da linha é a <see cref="HereFalsa"/>, por
/// configurar: cada teste conta os pedidos que chegaram aos dois, para provar que nada gasta quota sem necessidade.
/// </remarks>
public sealed class SimularPedidoPassageiroHttpTests : IDisposable
{
    private readonly HereFalsa here = new();
    private readonly AplicacaoReal app;
    private readonly Utilizador condutor;
    private readonly Utilizador aceite;
    private readonly Utilizador interessado;
    private readonly HttpClient clienteCondutor;
    private readonly HttpClient clienteAceite;
    private readonly HttpClient clienteInteressado;

    public SimularPedidoPassageiroHttpTests()
    {
        app = new AplicacaoReal(services =>
        {
            services.RemoveAll<IServicoGeometriaRotas>();
            services.AddSingleton<IServicoGeometriaRotas>(here.CriarServico(configurado: false));
        });

        condutor = app.CriarUtilizador("Condutor");
        aceite = app.CriarUtilizador("Aceite");
        interessado = app.CriarUtilizador("Interessado");
        clienteCondutor = app.ClienteDe(condutor);
        clienteAceite = app.ClienteDe(aceite);
        clienteInteressado = app.ClienteDe(interessado);
    }

    public void Dispose()
    {
        clienteCondutor.Dispose();
        clienteAceite.Dispose();
        clienteInteressado.Dispose();
        app.Dispose();
        here.Dispose();
    }

    private static string Caminho(Guid viagemId) => $"/api/v1/viagens/{viagemId}/simular-pedido";

    // Viagem Porto → Braga com um passageiro aceite que é recolhido na Maia (uma paragem de outro passageiro).
    private async Task<Guid> ViagemComUmAceiteNaMaiaAsync(int lugares = 3)
    {
        Guid viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor, lugares);
        await AplicacaoReal.AceitarAsync(clienteCondutor, await AplicacaoReal.PedirParticipacaoAsync(clienteAceite, viagemId, "Maia"));
        return viagemId;
    }

    // Latitude da recolha do passageiro aceite (a paragem que os outros não podem ver).
    private decimal LatitudeDaMaia() => app.NaBaseDeDados(db =>
        db.Participacoes.Single(p => p.PassageiroId == aceite.Id).RecolhaLatitude!.Value);

    // Todas as latitudes de uma rota da resposta: as dos pontos e as da linha (pares [latitude, longitude]).
    private static List<decimal> Latitudes(JsonElement rota)
    {
        var latitudes = new List<decimal>();

        foreach (JsonElement ponto in rota.GetProperty("pontosOrdenados").EnumerateArray())
            latitudes.Add(ponto.GetProperty("latitude").GetDecimal());

        foreach (JsonElement par in rota.GetProperty("geometria").EnumerateArray())
            latitudes.Add(par[0].GetDecimal());

        return latitudes;
    }

    private static async Task<string?> ErroAsync(HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("erro").GetString();
    }

    [Fact]
    /// <summary>
    /// P-13: quem não participa vê na rota simulada a origem, a sua recolha e o destino; a paragem do aceite (Maia)
    /// não aparece nos pontos, nos troços, nas retas nem em lado nenhum do corpo. Os totais são os da rota completa.
    /// </summary>
    public async Task I112_81_SimularPedido_NaoParticipante_VeSoOsSeusPontos()
    {
        Guid viagemId = await ViagemComUmAceiteNaMaiaAsync();

        using var resposta = await clienteInteressado.PostAsJsonAsync(Caminho(viagemId), new { recolha = "Famalicão" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        string corpo = await resposta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(corpo);
        JsonElement raiz = json.RootElement;
        JsonElement rota = raiz.GetProperty("rotaSimulada");
        JsonElement pontos = rota.GetProperty("pontosOrdenados");

        Assert.Equal(["Porto", "Famalicão", "Braga"], pontos.EnumerateArray().Select(p => p.GetProperty("local").GetString()));
        Assert.Equal(2, rota.GetProperty("segmentos").GetArrayLength());
        Assert.Equal(1, rota.GetProperty("segmentos")[1].GetProperty("deOrdem").GetInt32());
        Assert.Equal(2, rota.GetProperty("segmentos")[1].GetProperty("ateOrdem").GetInt32());

        // Sem HERE, as retas ligam só os três pontos visíveis.
        Assert.True(rota.GetProperty("geometriaAproximada").GetBoolean());
        Assert.Equal(3, rota.GetProperty("geometria").GetArrayLength());

        // Nada do passageiro aceite: nem o texto, nem as coordenadas, nem o id.
        Assert.DoesNotContain("Maia", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain(LatitudeDaMaia(), Latitudes(rota));
        Assert.DoesNotContain(aceite.Id.ToString(), corpo, StringComparison.OrdinalIgnoreCase);

        // Os totais são os da rota com as duas paragens (a soma dos troços visíveis bate certo).
        decimal somaDosTrocos = rota.GetProperty("segmentos").EnumerateArray().Sum(s => s.GetProperty("distanciaKm").GetDecimal());
        Assert.Equal(rota.GetProperty("distanciaTotalKm").GetDecimal(), somaDosTrocos);
        Assert.Equal(raiz.GetProperty("distanciaSimuladaKm").GetDecimal(), rota.GetProperty("distanciaTotalKm").GetDecimal());
        Assert.Equal("Famalicão", raiz.GetProperty("recolhaPassageiro").GetString());
        Assert.Equal("Braga", raiz.GetProperty("destinoPassageiro").GetString());
        Assert.Equal(2, rota.GetProperty("estimativaCusto").GetProperty("passageirosAceites").GetInt32());

        // Contrato: os campos da resposta, sem a ordem dos locais nem ids de pedidos.
        Assert.Equal(
            [
                "viagemId", "recolhaPassageiro", "destinoPassageiro", "distanciaOriginalKm", "duracaoOriginalMinutos",
                "distanciaSimuladaKm", "duracaoSimuladaMinutos", "desvioDistanciaKm", "desvioDuracaoMinutos", "rotaSimulada"
            ],
            raiz.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    /// <summary>O condutor da viagem recebe 403 com { erro } (ele simula os pedidos recebidos), sem chamar os mapas.</summary>
    public async Task I112_82_SimularPedido_Condutor_403SemChamarOsMapas()
    {
        Guid viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);
        int pedidosAntes = app.Mapas.PedidosRecebidos;

        using var resposta = await clienteCondutor.PostAsJsonAsync(Caminho(viagemId), new { recolha = "Famalicão" });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("O condutor simula os pedidos recebidos, não um pedido próprio.", await ErroAsync(resposta));
        Assert.Equal(pedidosAntes, app.Mapas.PedidosRecebidos);
    }

    [Fact]
    /// <summary>
    /// Sem pontos novos (a recolha é a origem da viagem, P-20): usa a rota guardada e as coordenadas da viagem, sem
    /// nenhum pedido ao Serviço de Mapas nem à HERE; o desvio é 0.
    /// </summary>
    public async Task I112_83_SimularPedido_SemPontosNovos_ZeroChamadasExternas()
    {
        Guid viagemId = await ViagemComUmAceiteNaMaiaAsync();
        int pedidosAntes = app.Mapas.PedidosRecebidos;
        int hereAntes = here.Chamadas;

        using var resposta = await clienteInteressado.PostAsJsonAsync(Caminho(viagemId), new { recolha = "porto" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(0m, json.RootElement.GetProperty("desvioDistanciaKm").GetDecimal());
        Assert.Equal(2, json.RootElement.GetProperty("rotaSimulada").GetProperty("pontosOrdenados").GetArrayLength());
        Assert.Equal(pedidosAntes, app.Mapas.PedidosRecebidos);
        Assert.Equal(hereAntes, here.Chamadas);
    }

    [Fact]
    /// <summary>Viagem cheia: 409 com a mensagem do pedido real, sem chamar os mapas.</summary>
    public async Task I112_84_SimularPedido_ViagemCheia_409()
    {
        Guid viagemId = await ViagemComUmAceiteNaMaiaAsync(lugares: 1);
        int pedidosAntes = app.Mapas.PedidosRecebidos;

        using var resposta = await clienteInteressado.PostAsJsonAsync(Caminho(viagemId), new { recolha = "Famalicão" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Não existem lugares disponíveis nesta viagem.", await ErroAsync(resposta));
        Assert.Equal(pedidosAntes, app.Mapas.PedidosRecebidos);
    }

    [Fact]
    /// <summary>RF-F004-06: quem já tem um pedido pendente na viagem recebe o 409 do pedido repetido, sem chamar os mapas.</summary>
    public async Task I112_85_SimularPedido_PedidoRepetido_409()
    {
        Guid viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);
        await AplicacaoReal.PedirParticipacaoAsync(clienteInteressado, viagemId, "Maia");
        int pedidosAntes = app.Mapas.PedidosRecebidos;

        using var resposta = await clienteInteressado.PostAsJsonAsync(Caminho(viagemId), new { recolha = "Famalicão" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Já existe um pedido ou participação ativa nesta viagem.", await ErroAsync(resposta));
        Assert.Equal(pedidosAntes, app.Mapas.PedidosRecebidos);
    }

    [Fact]
    /// <summary>A simulação não grava nada: participações, notificações e a viagem (rota, lugares, distância) ficam iguais.</summary>
    public async Task I112_86_SimularPedido_NadaGravado()
    {
        Guid viagemId = await ViagemComUmAceiteNaMaiaAsync();
        string antes = Retrato(viagemId);

        using var resposta = await clienteInteressado.PostAsJsonAsync(Caminho(viagemId), new { recolha = "Famalicão", destino = "Barcelos" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(antes, Retrato(viagemId));
    }

    // Estado da BD que a simulação não pode mudar.
    private string Retrato(Guid viagemId) => app.NaBaseDeDados(db =>
    {
        Viagem viagem = db.Viagens.Single(v => v.Id == viagemId);
        return string.Join(
            "|",
            db.Participacoes.Count(),
            db.Notificacoes.Count(),
            viagem.RotaPontos,
            viagem.LugaresDisponiveis,
            viagem.DistanciaKm,
            viagem.DuracaoMinutos);
    });

    [Fact]
    /// <summary>
    /// D-1 / P-13 em <c>GET /rotas/viagens/{id}</c>: cada aceite vê os pontos de todos os aceites; quem não participa
    /// (aqui com um pedido pendente) vê só a origem e o destino, sem o texto nem as coordenadas das paragens.
    /// </summary>
    public async Task I112_87_RotaDaViagem_AceiteVeTodos_NaoParticipanteNaoVe()
    {
        Guid viagemId = await ViagemComUmAceiteNaMaiaAsync();
        Utilizador segundo = app.CriarUtilizador("Segundo");
        using HttpClient clienteSegundo = app.ClienteDe(segundo);
        await AplicacaoReal.AceitarAsync(clienteCondutor, await AplicacaoReal.PedirParticipacaoAsync(clienteSegundo, viagemId, "Trofa"));
        await AplicacaoReal.PedirParticipacaoAsync(clienteInteressado, viagemId, "Famalicão");

        string vistaDoAceite = await clienteAceite.GetStringAsync($"/api/v1/rotas/viagens/{viagemId}");
        string vistaDoPendente = await clienteInteressado.GetStringAsync($"/api/v1/rotas/viagens/{viagemId}");

        using var aceiteJson = JsonDocument.Parse(vistaDoAceite);
        IEnumerable<string?> locaisDoAceite = aceiteJson.RootElement.GetProperty("pontosOrdenados").EnumerateArray()
            .Select(p => p.GetProperty("local").GetString());
        Assert.Contains("Maia", locaisDoAceite);
        Assert.Contains("Trofa", locaisDoAceite);

        using var pendenteJson = JsonDocument.Parse(vistaDoPendente);
        Assert.Equal(2, pendenteJson.RootElement.GetProperty("pontosOrdenados").GetArrayLength());
        Assert.DoesNotContain("Maia", vistaDoPendente, StringComparison.Ordinal);
        Assert.DoesNotContain("Trofa", vistaDoPendente, StringComparison.Ordinal);
        Assert.DoesNotContain(LatitudeDaMaia(), Latitudes(pendenteJson.RootElement));
        Assert.Contains(LatitudeDaMaia(), Latitudes(aceiteJson.RootElement));
    }
}

/// <summary>A simulação do passageiro com a HERE em baixo: os mapas passam para a reserva (OpenRouteService), D-5.</summary>
public sealed class SimularPedidoPassageiroReservaHttpTests : IDisposable
{
    private readonly HereMapasFalsa hereMapas = new() { Falha = HttpStatusCode.ServiceUnavailable };
    private readonly MapasFalsos reserva = new();
    private readonly AplicacaoReal app;

    public SimularPedidoPassageiroReservaHttpTests()
    {
        app = new AplicacaoReal(services =>
        {
            services.RemoveAll<IServicoMapasRotas>();
            services.AddSingleton<IServicoMapasRotas>(fornecedor => new ServicoMapasComReserva(
                hereMapas.CriarServico(),
                reserva.CriarServico(),
                fornecedor.GetRequiredService<IMemoryCache>(),
                NullLogger<ServicoMapasComReserva>.Instance));
        });
    }

    public void Dispose()
    {
        app.Dispose();
        hereMapas.Dispose();
        reserva.Dispose();
    }

    [Fact]
    /// <summary>HERE a responder 503: a geocodificação e a matriz da simulação vêm da reserva e a resposta é 200.</summary>
    public async Task I112_88_SimularPedido_HereEmBaixo_UsaAReserva()
    {
        Utilizador condutor = app.CriarUtilizador("Condutor");
        Utilizador interessado = app.CriarUtilizador("Interessado");
        using HttpClient clienteCondutor = app.ClienteDe(condutor);
        using HttpClient clienteInteressado = app.ClienteDe(interessado);
        Guid viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);
        int geocodificacoesAntes = reserva.ChamadasGeocodificacao;
        int rotasAntes = reserva.ChamadasRotas;

        using var resposta = await clienteInteressado.PostAsJsonAsync(
            $"/api/v1/viagens/{viagemId}/simular-pedido",
            new { recolha = "Famalicão" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(geocodificacoesAntes + 1, reserva.ChamadasGeocodificacao);
        Assert.Equal(rotasAntes + 1, reserva.ChamadasRotas);
        Assert.True(hereMapas.ChamadasGeocodificacao + hereMapas.ChamadasMatriz > 0);
    }
}
