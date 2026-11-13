using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Regras de <see cref="Utilizador"/>, <see cref="ConfirmacaoConta"/>, <see cref="Avaliacao"/> e <see cref="Notificacao"/> (modelo de classes §2.2).</summary>
public sealed class EntidadesTests
{
    private static readonly DateTime Agora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Utilizador_Registar_FicaPorConfirmar()
    {
        var utilizador = Utilizador.Registar("Ana", "ana@estg.ipp.pt");

        Assert.Equal("Ana", utilizador.Nome);
        Assert.Equal("ana@estg.ipp.pt", utilizador.Email);
        Assert.False(utilizador.EmailConfirmado);
    }

    [Fact]
    public void Utilizador_Confirmar_FicaConfirmado()
    {
        var utilizador = Utilizador.Registar("Ana", "ana@estg.ipp.pt");

        utilizador.Confirmar();

        Assert.True(utilizador.EmailConfirmado);
    }

    [Fact]
    public void Utilizador_AtualizarPerfil_MudaOsTresCampos()
    {
        var utilizador = Utilizador.Registar("Ana", "ana@estg.ipp.pt");

        utilizador.AtualizarPerfil("Ana Silva", "LEI", null);

        Assert.Equal("Ana Silva", utilizador.Nome);
        Assert.Equal("LEI", utilizador.Curso);
        Assert.Null(utilizador.Instituicao);
    }

    [Fact]
    public void ConfirmacaoConta_DentroDaValidade_PodeSerUsada()
    {
        var confirmacao = new ConfirmacaoConta { ExpiraEm = Agora.AddHours(1) };

        Assert.True(confirmacao.EstaValida(Agora));

        confirmacao.Utilizar(Agora);

        Assert.Equal(Agora, confirmacao.ConsumidoEm);
    }

    [Fact]
    public void ConfirmacaoConta_Expirada_ERecusada()
    {
        var confirmacao = new ConfirmacaoConta { ExpiraEm = Agora };

        Assert.False(confirmacao.EstaValida(Agora));
        Assert.Throws<RegraDeNegocioException>(() => confirmacao.Utilizar(Agora));
        Assert.Null(confirmacao.ConsumidoEm);
    }

    [Fact]
    public void ConfirmacaoConta_SoServeUmaVez()
    {
        var confirmacao = new ConfirmacaoConta { ExpiraEm = Agora.AddHours(1) };
        confirmacao.Utilizar(Agora);

        Assert.Throws<RegraDeNegocioException>(() => confirmacao.Utilizar(Agora));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void Avaliacao_ValorValido_DeUmACinco(int classificacao, bool esperado)
    {
        Assert.Equal(esperado, Avaliacao.ValorValido(classificacao));
    }

    [Fact]
    public void Avaliacao_Alterar_MudaSoOValor()
    {
        var avaliacao = Avaliacao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3);
        var criadaEm = avaliacao.CriadoEm;

        avaliacao.Alterar(5);

        Assert.Equal(5, avaliacao.Classificacao);
        Assert.Equal(criadaEm, avaliacao.CriadoEm);
    }

    [Fact]
    public void Avaliacao_ValorForaDeUmACinco_ERecusado()
    {
        Assert.Throws<RegraDeNegocioException>(() => Avaliacao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 6));
    }

    [Theory]
    [InlineData(TipoNotificacao.PedidoRecebido, true)]
    [InlineData(TipoNotificacao.PedidoAceite, true)]
    [InlineData(TipoNotificacao.PedidoRejeitado, true)]
    [InlineData(TipoNotificacao.AlteracaoViagem, false)]
    [InlineData(TipoNotificacao.Aviso, false)]
    [InlineData(TipoNotificacao.NovaMensagem, false)]
    public void Notificacao_OriginaPush_SoNosTresTiposDePedido(TipoNotificacao tipo, bool esperado)
    {
        Assert.Equal(esperado, new Notificacao { Tipo = tipo }.OriginaPush());
    }

    [Fact]
    public void Notificacao_MarcarComoLida_MantemAPrimeiraLeitura()
    {
        var notificacao = new Notificacao();

        notificacao.MarcarComoLida(Agora);
        notificacao.MarcarComoLida(Agora.AddHours(1));

        Assert.Equal(Agora, notificacao.LidaEm);
    }
}
