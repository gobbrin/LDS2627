using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Transições da <see cref="Participacao"/> (AD-001 §5) e o limite de cancelamento de P-03. Sem base de dados.</summary>
public sealed class ParticipacaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Participacao Com(EstadoParticipacao estado) => new() { Estado = estado };

    private static Viagem Partida(int horasAtePartida) => new() { PartidaEm = Agora.AddHours(horasAtePartida) };

    [Fact]
    public void NovaParticipacao_FicaPendente()
    {
        var participacao = new Participacao();

        Assert.True(participacao.EstaPendente);
        Assert.Null(participacao.AceiteEm);
    }

    [Fact]
    public void Aceitar_RegistaOMomento()
    {
        var participacao = new Participacao();

        participacao.Aceitar(Agora);

        Assert.True(participacao.EstaAceite);
        Assert.Equal(Agora, participacao.AceiteEm);
    }

    [Fact]
    public void Rejeitar_PassaARejeitada()
    {
        var participacao = new Participacao();

        participacao.Rejeitar();

        Assert.Equal(EstadoParticipacao.Rejeitada, participacao.Estado);
    }

    [Theory]
    [InlineData(EstadoParticipacao.Aceite)]
    [InlineData(EstadoParticipacao.Rejeitada)]
    [InlineData(EstadoParticipacao.Cancelada)]
    public void SoPendente_PodeSerDecidido(EstadoParticipacao estado)
    {
        Assert.Throws<RegraDeNegocioException>(() => Com(estado).Aceitar(Agora));
        Assert.Throws<RegraDeNegocioException>(() => Com(estado).Rejeitar());
        Assert.Throws<RegraDeNegocioException>(() => Com(estado).CancelarPorConclusao());
    }

    [Theory]
    [InlineData(EstadoParticipacao.Pendente, 1, true)]
    [InlineData(EstadoParticipacao.Pendente, -1, true)]
    [InlineData(EstadoParticipacao.Aceite, 1, true)]
    [InlineData(EstadoParticipacao.Aceite, -1, false)]
    [InlineData(EstadoParticipacao.Rejeitada, 1, false)]
    [InlineData(EstadoParticipacao.Cancelada, 1, false)]
    public void PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(EstadoParticipacao estado, int horasAtePartida, bool permitido)
    {
        var participacao = Com(estado);
        var viagem = Partida(horasAtePartida);

        Assert.Equal(permitido, participacao.PodeSerCanceladaPeloPassageiro(viagem, Agora));

        if (permitido)
        {
            participacao.CancelarPeloPassageiro(viagem, Agora);
            Assert.Equal(EstadoParticipacao.Cancelada, participacao.Estado);
        }
        else
        {
            Assert.Throws<RegraDeNegocioException>(() => participacao.CancelarPeloPassageiro(viagem, Agora));
            Assert.Equal(estado, participacao.Estado);
        }
    }

    [Theory]
    [InlineData(EstadoParticipacao.Pendente)]
    [InlineData(EstadoParticipacao.Aceite)]
    public void CancelarPorIndisponibilidade_PendentesEAceites(EstadoParticipacao estado)
    {
        var participacao = Com(estado);

        participacao.CancelarPorIndisponibilidade();

        Assert.Equal(EstadoParticipacao.Cancelada, participacao.Estado);
    }

    [Theory]
    [InlineData(EstadoParticipacao.Rejeitada)]
    [InlineData(EstadoParticipacao.Cancelada)]
    public void CancelarPorIndisponibilidade_JaTerminada_ERecusado(EstadoParticipacao estado)
    {
        Assert.Throws<RegraDeNegocioException>(() => Com(estado).CancelarPorIndisponibilidade());
    }

    [Fact]
    public void Cancelamento_MantemAceiteEm_ParaOHistorico()
    {
        var participacao = new Participacao();
        participacao.Aceitar(Agora);

        participacao.CancelarPorIndisponibilidade();

        Assert.Equal(Agora, participacao.AceiteEm);
    }

    [Fact]
    public void CancelarPorConclusao_SoPendentes()
    {
        var participacao = new Participacao();

        participacao.CancelarPorConclusao();

        Assert.Equal(EstadoParticipacao.Cancelada, participacao.Estado);
    }
}
