using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Erros de indisponibilizar e concluir uma viagem (RF-F002-09, RF-F002-11; <c>CicloDeVidaDaViagemService</c>, AE-02):
/// viagem que não existe, viagem de outro condutor e viagem que já não está ativa. Em nenhum caso a viagem muda.
/// SQLite em memória (ou SQL Server, no modo da CI), com o serviço real.
/// </summary>
public sealed class CicloDeVidaDaViagemTests : IDisposable
{
    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;
    private readonly Utilizador _condutor = new() { Nome = "Condutor", Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador _outro = new() { Nome = "Outro", Email = $"{Guid.NewGuid():N}@estg.ipp.pt", EmailConfirmado = true };

    public CicloDeVidaDaViagemTests()
    {
        _ligacao.Open();
        _db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseBaseDeTeste(_ligacao).Options);
        _db.Database.CriarEsquemaDeTeste();
        _db.Utilizadores.AddRange(_condutor, _outro);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _ligacao.Dispose();
    }

    // Viagem do condutor no estado indicado, com a partida há uma hora (já pode ser concluída).
    private async Task<Viagem> CriarViagemAsync(EstadoViagem estado)
    {
        var viagem = new Viagem
        {
            CondutorId = _condutor.Id,
            Origem = "Porto",
            OrigemLatitude = 41.15m,
            OrigemLongitude = -8.61m,
            Destino = "Braga",
            DestinoLatitude = 41.55m,
            DestinoLongitude = -8.42m,
            PartidaEm = DateTime.UtcNow.AddHours(-1),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3,
            Estado = estado
        };
        _db.Viagens.Add(viagem);
        await _db.SaveChangesAsync();
        return viagem;
    }

    private async Task<EstadoViagem> EstadoGravadoAsync(Guid viagemId)
    {
        _db.ChangeTracker.Clear();
        return (await _db.Viagens.AsNoTracking().SingleAsync(v => v.Id == viagemId)).Estado;
    }

    [Fact]
    /// <summary>Indisponibilizar ou concluir uma viagem que não existe dá 404 «Viagem não encontrada.».</summary>
    public async Task I112_109_ViagemInexistente_Devolve404()
    {
        var servico = ServicosDeTeste.CriarCicloDeVida(_db);

        Resultado indisponibilizar = await servico.IndisponibilizarAsync(Guid.NewGuid(), _condutor.Id, default);
        Resultado concluir = await servico.ConcluirAsync(Guid.NewGuid(), _condutor.Id, default);

        Assert.Equal(TipoErro.NaoEncontrado, indisponibilizar.Erro?.Tipo);
        Assert.Equal("Viagem não encontrada.", indisponibilizar.Erro?.Mensagem);
        Assert.Equal(TipoErro.NaoEncontrado, concluir.Erro?.Tipo);
    }

    [Fact]
    /// <summary>Indisponibilizar uma viagem já indisponível dá 409 e o estado fica como estava.</summary>
    public async Task I112_110_IndisponibilizarViagemNaoAtiva_Devolve409()
    {
        var viagem = await CriarViagemAsync(EstadoViagem.Indisponibilizada);

        Resultado resultado = await ServicosDeTeste.CriarCicloDeVida(_db).IndisponibilizarAsync(viagem.Id, _condutor.Id, default);

        Assert.Equal(TipoErro.Conflito, resultado.Erro?.Tipo);
        Assert.Equal("A viagem já não está ativa.", resultado.Erro?.Mensagem);
        Assert.Equal(EstadoViagem.Indisponibilizada, await EstadoGravadoAsync(viagem.Id));
    }

    [Fact]
    /// <summary>Só o condutor conclui a viagem: outro utilizador recebe 403 e a viagem continua ativa.</summary>
    public async Task I112_111_ConcluirViagemDeOutroCondutor_Devolve403()
    {
        var viagem = await CriarViagemAsync(EstadoViagem.Ativa);

        Resultado resultado = await ServicosDeTeste.CriarCicloDeVida(_db).ConcluirAsync(viagem.Id, _outro.Id, default);

        Assert.Equal(TipoErro.Proibido, resultado.Erro?.Tipo);
        Assert.Equal("Não tens permissão para gerir esta viagem.", resultado.Erro?.Mensagem);
        Assert.Equal(EstadoViagem.Ativa, await EstadoGravadoAsync(viagem.Id));
    }
}
