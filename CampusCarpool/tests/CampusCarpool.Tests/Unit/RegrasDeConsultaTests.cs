using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Regras puras das consultas de viagens: visibilidade (RF-F003-02, A7) e classificação média (P-10).</summary>
public sealed class RegrasDeConsultaTests
{
    private static readonly DateTime Agora = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Condutor = Guid.NewGuid();

    private static Viagem Viagem(string estado = nameof(EstadoViagem.Ativa), int lugares = 2, int horas = 1) => new()
    {
        CondutorId = Condutor,
        Estado = estado,
        LugaresDisponiveis = lugares,
        PartidaEm = Agora.AddHours(horas)
    };

    [Fact]
    public void ViagemAtivaFuturaComLugares_EPesquisavel()
    {
        Assert.True(PoliticaDeVisibilidade.Pesquisavel(Viagem(), Agora));
    }

    [Theory]
    [InlineData(nameof(EstadoViagem.Concluida), 2, 1)]
    [InlineData(nameof(EstadoViagem.Ativa), 0, 1)]
    [InlineData(nameof(EstadoViagem.Ativa), 2, -1)]
    public void ViagemTerminadaCheiaOuJaPartida_NaoEPesquisavel(string estado, int lugares, int horas)
    {
        Assert.False(PoliticaDeVisibilidade.Pesquisavel(Viagem(estado, lugares, horas), Agora));
    }

    [Fact]
    public void Condutor_VeSempreASuaViagem()
    {
        var viagem = Viagem(nameof(EstadoViagem.Concluida));

        Assert.True(PoliticaDeVisibilidade.VisivelSemPedido(viagem, Condutor, Agora));
    }

    [Fact]
    public void OutroUtilizador_SoVeViagemPesquisavel()
    {
        var outro = Guid.NewGuid();

        Assert.True(PoliticaDeVisibilidade.VisivelSemPedido(Viagem(), outro, Agora));
        Assert.False(PoliticaDeVisibilidade.VisivelSemPedido(Viagem(lugares: 0), outro, Agora));
    }

    [Fact]
    public void SemAvaliacoes_NaoHaMedia()
    {
        Assert.Null(CalculadoraDeClassificacao.Media([]));
    }

    [Fact]
    public void Media_TemUmaCasaDecimal_ArredondadaParaCima()
    {
        Assert.Equal(4.3m, CalculadoraDeClassificacao.Media([4, 4, 4, 5]));
        Assert.Equal(3.7m, CalculadoraDeClassificacao.Media([3, 4, 4]));
    }
}
