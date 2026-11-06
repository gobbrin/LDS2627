using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Estados e lugares da <see cref="Viagem"/> (AD-001, modelo de classes §2.2). Sem base de dados.</summary>
public sealed class ViagemTests
{
    private static readonly DateTime Agora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static readonly LocaisViagem Locais = new(
        "Felgueiras",
        new Coordenadas(41.3m, -8.2m),
        "Porto",
        new Coordenadas(41.1m, -8.6m));

    private static Viagem NovaViagem(
        EstadoViagem estado = EstadoViagem.Ativa,
        int oferecidos = 3,
        int disponiveis = 3,
        int horasAtePartida = 1,
        int? duracao = 30) => new()
        {
            Estado = estado,
            LugaresOferecidos = oferecidos,
            LugaresDisponiveis = disponiveis,
            PartidaEm = Agora.AddHours(horasAtePartida),
            DuracaoMinutos = duracao
        };

    [Fact]
    public void Criar_FicaAtivaComTodosOsLugaresLivres()
    {
        var condutor = Guid.NewGuid();

        var viagem = Viagem.Criar(condutor, Locais, Agora.AddDays(1), 4, 25);

        Assert.Equal(condutor, viagem.CondutorId);
        Assert.Equal(EstadoViagem.Ativa, viagem.Estado);
        Assert.Equal(4, viagem.LugaresOferecidos);
        Assert.Equal(4, viagem.LugaresDisponiveis);
        Assert.Equal("Felgueiras", viagem.Origem);
        Assert.Equal(41.1m, viagem.DestinoLatitude);
        Assert.Equal(25, viagem.DuracaoMinutos);
        Assert.Null(viagem.RotaPontos);
    }

    [Fact]
    public void Alterar_MantemOcupadosERecalculaDisponiveis()
    {
        var viagem = NovaViagem(oferecidos: 3, disponiveis: 1);

        viagem.Alterar(Locais, Agora.AddDays(2), 5, 40, Agora);

        Assert.Equal(5, viagem.LugaresOferecidos);
        Assert.Equal(3, viagem.LugaresDisponiveis);
        Assert.Equal(2, viagem.LugaresOcupados);
        Assert.Equal(40, viagem.DuracaoMinutos);
        Assert.Equal("Porto", viagem.Destino);
    }

    [Fact]
    public void Alterar_LugaresAbaixoDosOcupados_ERecusado()
    {
        var viagem = NovaViagem(oferecidos: 3, disponiveis: 0);

        Assert.Throws<RegraDeNegocioException>(() => viagem.Alterar(Locais, Agora.AddDays(2), 2, 40, Agora));
        Assert.Equal(3, viagem.LugaresOferecidos);
    }

    [Theory]
    [InlineData(EstadoViagem.Indisponibilizada, 1)]
    [InlineData(EstadoViagem.Concluida, 1)]
    [InlineData(EstadoViagem.Ativa, -1)]
    public void Alterar_ViagemNaoAtivaOuJaPartida_ERecusado(EstadoViagem estado, int horasAtePartida)
    {
        var viagem = NovaViagem(estado, horasAtePartida: horasAtePartida);

        Assert.Throws<RegraDeNegocioException>(() => viagem.Alterar(Locais, Agora.AddDays(2), 3, 40, Agora));
    }

    [Fact]
    public void Indisponibilizar_LibertaTodosOsLugares()
    {
        var viagem = NovaViagem(oferecidos: 3, disponiveis: 1);

        viagem.Indisponibilizar();

        Assert.Equal(EstadoViagem.Indisponibilizada, viagem.Estado);
        Assert.Equal(3, viagem.LugaresDisponiveis);
    }

    [Theory]
    [InlineData(EstadoViagem.Indisponibilizada)]
    [InlineData(EstadoViagem.Concluida)]
    public void Indisponibilizar_ViagemNaoAtiva_ERecusado(EstadoViagem estado)
    {
        Assert.Throws<RegraDeNegocioException>(() => NovaViagem(estado).Indisponibilizar());
    }

    [Fact]
    public void Concluir_DepoisDaPartida_FicaConcluida()
    {
        var viagem = NovaViagem(horasAtePartida: -1);

        viagem.Concluir(Agora);

        Assert.True(viagem.EstaConcluida);
    }

    [Theory]
    [InlineData(EstadoViagem.Ativa, 1)]
    [InlineData(EstadoViagem.Indisponibilizada, -1)]
    [InlineData(EstadoViagem.Concluida, -1)]
    public void Concluir_AntesDaPartidaOuNaoAtiva_ERecusado(EstadoViagem estado, int horasAtePartida)
    {
        Assert.Throws<RegraDeNegocioException>(() => NovaViagem(estado, horasAtePartida: horasAtePartida).Concluir(Agora));
    }

    [Theory]
    [InlineData(-60, 60, true)]
    [InlineData(-59, 60, false)]
    public void DeveConcluir_SoDepoisDaPartidaMaisADuracao(int minutosAtePartida, int duracao, bool esperado)
    {
        var viagem = new Viagem { PartidaEm = Agora.AddMinutes(minutosAtePartida), DuracaoMinutos = duracao };

        Assert.Equal(esperado, viagem.DeveConcluir(Agora));
    }

    [Fact]
    public void DeveConcluir_SemDuracao_ConcluiAHoraDePartida()
    {
        Assert.True(new Viagem { PartidaEm = Agora, DuracaoMinutos = null }.DeveConcluir(Agora));
    }

    [Fact]
    public void DeveConcluir_ViagemNaoAtiva_Falso()
    {
        Assert.False(NovaViagem(EstadoViagem.Indisponibilizada, horasAtePartida: -5).DeveConcluir(Agora));
    }

    [Fact]
    public void OcuparELibertarLugar_AcompanhamOsDisponiveis()
    {
        var viagem = NovaViagem(oferecidos: 2, disponiveis: 2);

        viagem.OcuparLugar();
        Assert.Equal(1, viagem.LugaresDisponiveis);

        viagem.LibertarLugar();
        Assert.Equal(2, viagem.LugaresDisponiveis);
    }

    [Fact]
    public void OcuparLugar_SemLugares_ERecusado()
    {
        Assert.Throws<RegraDeNegocioException>(() => NovaViagem(oferecidos: 1, disponiveis: 0).OcuparLugar());
    }

    [Fact]
    public void LibertarLugar_ComTodosLivres_ERecusado()
    {
        Assert.Throws<RegraDeNegocioException>(() => NovaViagem(oferecidos: 2, disponiveis: 2).LibertarLugar());
    }

    [Theory]
    [InlineData(3, 2, 1, true)]
    [InlineData(3, 3, 1, false)]
    [InlineData(1, -1, 2, false)]
    public void LugaresConsistentes_ComparaComOsAceites(int oferecidos, int disponiveis, int aceites, bool esperado)
    {
        Assert.Equal(esperado, NovaViagem(oferecidos: oferecidos, disponiveis: disponiveis).LugaresConsistentes(aceites));
    }

    [Fact]
    public void AtualizarRota_GuardaARotaEADistanciaMasNaoADuracao()
    {
        var viagem = NovaViagem(duracao: 30);

        viagem.AtualizarRota("[]", 12.5m);

        Assert.Equal("[]", viagem.RotaPontos);
        Assert.Equal(12.5m, viagem.DistanciaKm);
        Assert.Equal(30, viagem.DuracaoMinutos);
    }

    [Fact]
    public void MarcarRotaPorRecalcular_LimpaARotaEMantemADistancia()
    {
        var viagem = NovaViagem();
        viagem.AtualizarRota("[]", 12.5m);

        viagem.MarcarRotaPorRecalcular();

        Assert.Null(viagem.RotaPontos);
        Assert.Equal(12.5m, viagem.DistanciaKm);
    }
}
