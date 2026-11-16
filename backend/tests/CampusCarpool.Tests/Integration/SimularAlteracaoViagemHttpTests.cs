using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// <c>POST /api/v1/viagens/{id}/simular-alteracao</c> pelo pipeline HTTP real (<see cref="AplicacaoReal"/>): JWT,
/// validação do corpo, mapa de erros para <c>{ erro }</c> e serialização (WEB-01, MOB-01).
/// </summary>
/// <remarks>A HERE é a <see cref="HereFalsa"/>, configurada, para provar que a simulação não a chama.</remarks>
public sealed class SimularAlteracaoViagemHttpTests : IDisposable
{
    private readonly HereFalsa here = new();
    private readonly AplicacaoReal app;
    private readonly Utilizador condutor;
    private readonly Utilizador passageiro;
    private readonly HttpClient clienteCondutor;
    private readonly HttpClient clientePassageiro;

    public SimularAlteracaoViagemHttpTests()
    {
        app = new AplicacaoReal(services =>
        {
            services.RemoveAll<IServicoGeometriaRotas>();
            services.AddSingleton<IServicoGeometriaRotas>(here.CriarServico());
        });

        condutor = app.CriarUtilizador("Condutor");
        passageiro = app.CriarUtilizador("Passageiro");
        clienteCondutor = app.ClienteDe(condutor);
        clientePassageiro = app.ClienteDe(passageiro);
    }

    public void Dispose()
    {
        clienteCondutor.Dispose();
        clientePassageiro.Dispose();
        app.Dispose();
    }

    private static string Caminho(Guid viagemId) => $"/api/v1/viagens/{viagemId}/simular-alteracao";

    // Lê o texto de { erro } de uma resposta de erro da API.
    private static async Task<string?> ErroAsync(HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("erro").GetString();
    }

    [Fact]
    /// <summary>Condutor com um passageiro aceite: 200 com a paragem e o custo; a viagem na BD fica igual e a HERE não é chamada.</summary>
    public async Task SimularAlteracao_Condutor_200SemGravarNemChamarAHere()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);
        await AplicacaoReal.AceitarAsync(
            clienteCondutor,
            await AplicacaoReal.PedirParticipacaoAsync(clientePassageiro, viagemId, "Maia"));
        string? rotaAntes = app.NaBaseDeDados(db => db.Viagens.Single(v => v.Id == viagemId).RotaPontos);
        int chamadasHere = here.Chamadas;

        using var resposta = await clienteCondutor.PostAsJsonAsync(Caminho(viagemId), new { origem = "Porto", destino = "Guimarães" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        JsonElement rota = json.RootElement;
        Assert.Equal(3, rota.GetProperty("pontosOrdenados").GetArrayLength());
        Assert.Equal("Recolha", rota.GetProperty("pontosOrdenados")[1].GetProperty("tipo").GetString());
        Assert.Equal(1, rota.GetProperty("estimativaCusto").GetProperty("passageirosAceites").GetInt32());
        Assert.True(rota.GetProperty("geometriaAproximada").GetBoolean());

        // Nada gravado: o destino e a rota guardada são os de antes.
        Assert.Equal("Braga", app.NaBaseDeDados(db => db.Viagens.Single(v => v.Id == viagemId).Destino));
        Assert.Equal(rotaAntes, app.NaBaseDeDados(db => db.Viagens.Single(v => v.Id == viagemId).RotaPontos));
        Assert.Equal(chamadasHere, here.Chamadas);
    }

    [Fact]
    /// <summary>Quem não é o condutor recebe 403 com { erro }.</summary>
    public async Task SimularAlteracao_NaoCondutor_403()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);

        using var resposta = await clientePassageiro.PostAsJsonAsync(Caminho(viagemId), new { destino = "Guimarães" });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("Não tens permissão para gerir esta viagem.", await ErroAsync(resposta));
    }

    [Fact]
    /// <summary>Viagem inexistente: 404 com { erro }.</summary>
    public async Task SimularAlteracao_ViagemInexistente_404()
    {
        using var resposta = await clienteCondutor.PostAsJsonAsync(Caminho(Guid.NewGuid()), new { destino = "Guimarães" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("Viagem não encontrada.", await ErroAsync(resposta));
    }

    [Fact]
    /// <summary>Um campo que não existe no corpo (por exemplo, <c>lugares</c>) dá 400, como nas outras rotas.</summary>
    public async Task SimularAlteracao_CampoAMais_400()
    {
        var viagemId = await AplicacaoReal.CriarViagemAsync(clienteCondutor);

        using var resposta = await clienteCondutor.PostAsJsonAsync(Caminho(viagemId), new { destino = "Guimarães", lugares = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ErroAsync(resposta)));
    }

    [Fact]
    /// <summary>Sem sessão: 401 sem corpo, como as outras rotas protegidas.</summary>
    public async Task SimularAlteracao_SemSessao_401()
    {
        using var anonimo = app.CreateClient();

        using var resposta = await anonimo.PostAsJsonAsync(Caminho(Guid.NewGuid()), new { destino = "Guimarães" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}

/// <summary>A mesma rota com o Serviço de Mapas sem chave: 503 com { erro }, como nas outras operações que dependem dele.</summary>
public sealed class SimularAlteracaoViagemSemMapasHttpTests : IDisposable
{
    private readonly AplicacaoReal app = new(services =>
    {
        services.RemoveAll<IServicoMapasRotas>();
        services.AddSingleton<IServicoMapasRotas>(new MapasFalsos().CriarServico(configurado: false));
    });

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task SimularAlteracao_SemMapas_503()
    {
        var condutor = app.CriarUtilizador("Condutor");

        // Sem mapas não se cria a viagem pela API: grava-se diretamente, já com as coordenadas.
        var locais = new LocaisViagem("Porto", new Coordenadas(41.15m, -8.61m), "Braga", new Coordenadas(41.55m, -8.42m));
        var viagem = Viagem.Criar(condutor.Id, locais, DateTime.UtcNow.AddDays(1), 3, 45);
        app.NaBaseDeDados(db =>
        {
            db.Viagens.Add(viagem);
            return db.SaveChanges();
        });
        using var cliente = app.ClienteDe(condutor);

        using var resposta = await cliente.PostAsJsonAsync(
            $"/api/v1/viagens/{viagem.Id}/simular-alteracao",
            new { origem = "Porto", destino = "Guimarães" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Serviço de mapas não configurado.", json.RootElement.GetProperty("erro").GetString());
    }
}
