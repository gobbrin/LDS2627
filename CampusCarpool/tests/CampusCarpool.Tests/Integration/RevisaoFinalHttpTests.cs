using System.Net;
using System.Net.Http.Json;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Defeitos encontrados na revisão final do código, pelo pipeline real (<see cref="AplicacaoReal"/>).</summary>
public sealed class RevisaoFinalHttpTests
{
    [Fact]
    public async Task I110_150_ViagemIndisponibilizadaDuranteAGeocodificacao_RecusaOPedido()
    {
        using var app = new AplicacaoReal();
        using var condutor = app.ClienteDe(app.CriarUtilizador("Condutor"));
        using var passageiro = app.ClienteDe(app.CriarUtilizador("Passageiro"));
        var viagemId = await AplicacaoReal.CriarViagemAsync(condutor);

        app.Mapas.AntesDeGeocodificar = () => app.NaBaseDeDados(db => db.Viagens
            .Where(v => v.Id == viagemId)
            .ExecuteUpdate(s => s.SetProperty(v => v.Estado, EstadoViagem.Indisponibilizada)));

        using var resposta = await passageiro.PostAsJsonAsync(
            "/api/v1/pedidos",
            new { viagemId, recolha = "Maia", destino = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.False(app.NaBaseDeDados(db => db.Participacoes.Any(p => p.ViagemId == viagemId)));
    }

    [Fact]
    public async Task I110_151_PaginaDasNotificacoesDemasiadoGrande_Devolve400()
    {
        using var app = new AplicacaoReal();
        using var cliente = app.ClienteDe(app.CriarUtilizador("Aluno"));

        using var resposta = await cliente.GetAsync($"/api/v1/notificacoes?pagina={int.MaxValue}&tamanhoPagina=100");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task I110_152_Avaliacoes_SoARecusaDoParContaComoConflito()
    {
        using var app = new AplicacaoReal();
        var avaliador = app.CriarUtilizador("Avaliador");
        var avaliado = app.CriarUtilizador("Avaliado");
        using var condutor = app.ClienteDe(avaliado);
        var viagemId = await AplicacaoReal.CriarViagemAsync(condutor);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>();
        var repositorio = new RepositorioAvaliacoes(db);

        Assert.True(await repositorio.GuardarAsync(Avaliacao.Criar(viagemId, avaliador.Id, avaliado.Id, 5), CancellationToken.None));
        Assert.False(await repositorio.GuardarAsync(Avaliacao.Criar(viagemId, avaliador.Id, avaliado.Id, 4), CancellationToken.None));

        // Ao alterar, o par existe sempre: uma recusa real não pode passar por conflito.
        var existente = await db.Avaliacoes.SingleAsync(a => a.AvaliadorId == avaliador.Id);
        db.Entry(existente).Property(a => a.Classificacao).CurrentValue = 9;
        await Assert.ThrowsAsync<DbUpdateException>(() => repositorio.GuardarAsync(existente, CancellationToken.None));
        db.ChangeTracker.Clear();

        // A viagem não existe: a chave estrangeira recusa, e isso não é um conflito.
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repositorio.GuardarAsync(Avaliacao.Criar(Guid.NewGuid(), avaliado.Id, avaliador.Id, 5), CancellationToken.None));
    }

    [Fact]
    public async Task I110_154_PedidoLigaANotificacaoAViagem_MostraOsPontos_EOPerfilTemClassificacao()
    {
        using var app = new AplicacaoReal();
        var aluno = app.CriarUtilizador("Passageiro");
        using var condutor = app.ClienteDe(app.CriarUtilizador("Condutor"));
        using var passageiro = app.ClienteDe(aluno);
        var viagemId = await AplicacaoReal.CriarViagemAsync(condutor);
        await AplicacaoReal.PedirParticipacaoAsync(passageiro, viagemId);

        Assert.True(app.NaBaseDeDados(db => db.Notificacoes.Any(n => n.Tipo == TipoNotificacao.PedidoRecebido && n.ViagemId == viagemId)));

        var historico = await passageiro.GetStringAsync("/api/v1/historico");
        Assert.Contains("\"pontoRecolha\":\"Maia\"", historico, StringComparison.Ordinal);

        var perfil = await passageiro.GetStringAsync("/api/v1/utilizadores/me");
        Assert.Contains("\"totalAvaliacoes\":0", perfil, StringComparison.Ordinal);
    }
}
