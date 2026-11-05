using CampusCarpool.Api.Services;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Ordem dos pontos da rota (P-07, RF-F004-04, DEF-BE-022) e formato guardado na viagem.</summary>
/// <remarks>Usa o <see cref="MapasFalsos"/>: os índices em <c>Duracao</c> são os dos pontos enviados à Matrix API.</remarks>
public sealed class OtimizadorRotasTests
{
    private static readonly Guid Passageiro1 = Guid.NewGuid();
    private static readonly Guid Passageiro2 = Guid.NewGuid();

    private static PontoRota Ponto(string local, string tipo = TiposPontoRota.Recolha, Guid? participacao = null) =>
        new(participacao, tipo, local, new Coordenadas(41m, local.Length));

    private static readonly PontoRota Origem = Ponto("Origem da viagem", TiposPontoRota.Origem);
    private static readonly PontoRota Destino = Ponto("Destino da viagem com outro tamanho", TiposPontoRota.DestinoViagem);

    private static List<string> Locais(ResultadoRotaComPontos rota) => rota.PontosOrdenados.Select(p => p.Local).ToList();

    [Fact]
    public async Task SemPontosAdicionais_DevolveOrigemEDestino()
    {
        var mapas = new MapasFalsos();

        var rota = await OtimizadorRotas.CalcularAsync(mapas.CriarServico(), Origem, Destino, [], default);

        Assert.Equal([Origem.Local, Destino.Local], Locais(rota!));
        Assert.Single(rota!.Segmentos);
        Assert.Equal(1, mapas.ChamadasRotas);
    }

    [Fact]
    /// <summary>P-07: uma escolha gulosa (a paragem mais próxima da origem primeiro) daria uma rota mais longa.</summary>
    public async Task EscolheMenorDuracaoTotal_NaoOVizinhoMaisProximo()
    {
        var perto = Ponto("A", participacao: Passageiro1);
        var longe = Ponto("BB", participacao: Passageiro2);

        // Índices: 0 origem, 1 "A", 2 "BB", 3 destino.
        var duracoes = new Dictionary<(int, int), double>
        {
            [(0, 1)] = 60,
            [(1, 2)] = 900,
            [(2, 3)] = 60,
            [(0, 2)] = 120,
            [(2, 1)] = 60,
            [(1, 3)] = 60
        };
        var mapas = new MapasFalsos { Duracao = (i, j) => duracoes.GetValueOrDefault((i, j), 5000) };

        var rota = await OtimizadorRotas.CalcularAsync(mapas.CriarServico(), Origem, Destino, [perto, longe], default);

        Assert.Equal([Origem.Local, "BB", "A", Destino.Local], Locais(rota!));
        Assert.Equal(4, rota!.DuracaoTotalMinutos);
    }

    [Fact]
    /// <summary>RF-F004-04: a recolha vem antes do destino, mesmo que a ordem inversa fosse mais rápida.</summary>
    public async Task RecolhaAntesDoDestinoDoPassageiro()
    {
        var recolha = Ponto("A", TiposPontoRota.Recolha, Passageiro1);
        var destino = Ponto("BB", TiposPontoRota.DestinoPassageiro, Passageiro1);

        // A ordem destino → recolha seria a mais rápida, mas não é válida.
        var duracoes = new Dictionary<(int, int), double> { [(0, 2)] = 1, [(2, 1)] = 1, [(1, 3)] = 1 };
        var mapas = new MapasFalsos { Duracao = (i, j) => duracoes.GetValueOrDefault((i, j), 1000) };

        var rota = await OtimizadorRotas.CalcularAsync(mapas.CriarServico(), Origem, Destino, [destino, recolha], default);

        Assert.Equal([Origem.Local, "A", "BB", Destino.Local], Locais(rota!));
    }

    [Fact]
    /// <summary>P-20: sem recolha própria, o passageiro entra na origem, por isso o destino não depende de nenhum ponto.</summary>
    public async Task DestinoSemRecolha_NaoTemPrecedencia()
    {
        var destino = Ponto("BB", TiposPontoRota.DestinoPassageiro, Passageiro1);
        var outraRecolha = Ponto("A", TiposPontoRota.Recolha, Passageiro2);

        var duracoes = new Dictionary<(int, int), double> { [(0, 1)] = 1, [(1, 2)] = 1, [(2, 3)] = 1 };
        var mapas = new MapasFalsos { Duracao = (i, j) => duracoes.GetValueOrDefault((i, j), 1000) };

        var rota = await OtimizadorRotas.CalcularAsync(mapas.CriarServico(), Origem, Destino, [destino, outraRecolha], default);

        Assert.Equal([Origem.Local, "BB", "A", Destino.Local], Locais(rota!));
    }

    [Fact]
    public async Task MaisDe16PontosAdicionais_ERecusado()
    {
        var pontos = Enumerable.Range(0, 17).Select(i => Ponto($"P{i}")).ToList();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            OtimizadorRotas.CalcularAsync(new MapasFalsos().CriarServico(), Origem, Destino, pontos, default));
    }

    [Fact]
    /// <summary>RF-F003-03: um ponto só é adicional se não coincidir com a origem nem com o destino da viagem.</summary>
    public async Task RotaGuardada_ConservaARotaEContaOsPontosAdicionais()
    {
        var adicional = Ponto("A", TiposPontoRota.Recolha, Passageiro1);
        var igualAoDestino = new PontoRota(Passageiro1, TiposPontoRota.DestinoPassageiro, "Mesmo sítio", Destino.Coordenadas);
        var rota = await OtimizadorRotas.CalcularAsync(
            new MapasFalsos().CriarServico(), Origem, Destino, [adicional, igualAoDestino], default);

        var json = RotaGuardada.Serializar(rota!);
        var lida = RotaGuardada.Ler(json);

        Assert.Equal(rota!.PontosOrdenados, lida.PontosOrdenados);
        Assert.Equal(rota.Segmentos, lida.Segmentos);
        Assert.Equal(rota.DistanciaTotalKm, lida.DistanciaTotalKm);
        Assert.Equal(rota.DuracaoTotalMinutos, lida.DuracaoTotalMinutos);
        Assert.Equal(1, RotaGuardada.ContarAdicionais(json));
    }
}
