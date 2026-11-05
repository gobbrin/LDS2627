using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.BackgroundJobs;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

// I110 — DEF-BE-009: conclusão automática das viagens (UC-F002-05; RF-F002-12, RF-F004-19, P-04).
// O serviço é chamado diretamente com um "agora" fixo, sobre SQLite em memória;
// o temporizador (TrabalhadorConclusaoViagens) só é verificado na configuração.
public sealed class ConclusaoViagensTests : IDisposable
{
    private static readonly DateTime Agora = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly CampusCarpoolDbContext _db;
    private readonly Utilizador _condutor;

    public ConclusaoViagensTests()
    {
        _ligacao.Open();
        _db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(_ligacao).Options);
        _db.Database.EnsureCreated();
        _condutor = new Utilizador { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
        _db.Utilizadores.Add(_condutor);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _ligacao.Dispose();
    }

    private ConclusaoViagensService CriarServico() =>
        new(new RepositorioViagens(_db), new RepositorioParticipacoes(_db), new UnidadeDeTrabalhoEfCore(_db), NullLogger<ConclusaoViagensService>.Instance);

    private Viagem CriarViagem(DateTime partidaEm, int? duracaoMinutos, string estado = nameof(EstadoViagem.Ativa))
    {
        var viagem = new Viagem
        {
            CondutorId = _condutor.Id,
            Origem = "ESTG",
            Destino = "Porto",
            PartidaEm = partidaEm,
            DuracaoMinutos = duracaoMinutos,
            LugaresOferecidos = 3,
            LugaresDisponiveis = 2,
            Estado = estado
        };
        _db.Viagens.Add(viagem);
        return viagem;
    }

    private async Task<string> EstadoAsync(Guid viagemId) =>
        (await _db.Viagens.AsNoTracking().SingleAsync(v => v.Id == viagemId)).Estado;

    [Fact]
    public async Task I110_11_ViagemTerminada_ConcluidaPendentesCanceladosAceitesMantidos()
    {
        var viagem = CriarViagem(Agora.AddMinutes(-90), duracaoMinutos: 60);
        var passageiroA = new Utilizador { Nome = "Passageiro A", Email = "a@estg.ipp.pt", EmailConfirmado = true };
        var passageiroB = new Utilizador { Nome = "Passageiro B", Email = "b@estg.ipp.pt", EmailConfirmado = true };
        _db.Utilizadores.AddRange(passageiroA, passageiroB);
        var aceite = new Participacao { ViagemId = viagem.Id, PassageiroId = passageiroA.Id, Estado = EstadoParticipacao.Aceite };
        var pendente = new Participacao { ViagemId = viagem.Id, PassageiroId = passageiroB.Id, Estado = EstadoParticipacao.Pendente };
        _db.Participacoes.AddRange(aceite, pendente);
        await _db.SaveChangesAsync();

        Assert.Equal(1, await CriarServico().ConcluirViagensTerminadasAsync(Agora, default));

        Assert.Equal(nameof(EstadoViagem.Concluida), await EstadoAsync(viagem.Id));
        var participacoes = await _db.Participacoes.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Estado);
        Assert.Equal(EstadoParticipacao.Aceite, participacoes[aceite.Id]);
        Assert.Equal(EstadoParticipacao.Cancelada, participacoes[pendente.Id]);
    }

    [Fact]
    public async Task I110_12_ViagemPartidaMasDentroDaDuracao_ContinuaAtiva()
    {
        // Partiu há 30 min, mas a duração estimada é de 60 min (P-04).
        var viagem = CriarViagem(Agora.AddMinutes(-30), duracaoMinutos: 60);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await CriarServico().ConcluirViagensTerminadasAsync(Agora, default));
        Assert.Equal(nameof(EstadoViagem.Ativa), await EstadoAsync(viagem.Id));
    }

    [Fact]
    public async Task I110_13_ViagemSemDuracaoEstimada_ConcluidaAHoraDePartida()
    {
        var viagem = CriarViagem(Agora.AddMinutes(-1), duracaoMinutos: null);
        await _db.SaveChangesAsync();

        Assert.Equal(1, await CriarServico().ConcluirViagensTerminadasAsync(Agora, default));
        Assert.Equal(nameof(EstadoViagem.Concluida), await EstadoAsync(viagem.Id));
    }

    [Fact]
    public async Task I110_14_ViagensFuturasOuNaoAtivas_NaoSaoAlteradas()
    {
        var futura = CriarViagem(Agora.AddHours(1), duracaoMinutos: 30);
        var indisponivel = CriarViagem(Agora.AddDays(-1), duracaoMinutos: 30, nameof(EstadoViagem.Indisponibilizada));
        await _db.SaveChangesAsync();

        Assert.Equal(0, await CriarServico().ConcluirViagensTerminadasAsync(Agora, default));
        Assert.Equal(nameof(EstadoViagem.Ativa), await EstadoAsync(futura.Id));
        Assert.Equal(nameof(EstadoViagem.Indisponibilizada), await EstadoAsync(indisponivel.Id));
    }

    [Fact]
    public async Task I110_15_FalhaNumaViagem_NaoImpedeAsRestantes()
    {
        var comFalha = CriarViagem(Agora.AddHours(-3), duracaoMinutos: 30);
        var normal = CriarViagem(Agora.AddHours(-2), duracaoMinutos: 30);
        await _db.SaveChangesAsync();

        // Um gatilho do SQLite faz falhar só a gravação da primeira viagem (UC-F002-05, E1).
        var gatilho = $"CREATE TRIGGER falha_viagem BEFORE UPDATE ON Viagens WHEN old.Id = '{comFalha.Id.ToString().ToUpperInvariant()}' " +
            "BEGIN SELECT RAISE(ABORT, 'falha simulada'); END;";
        await _db.Database.ExecuteSqlRawAsync(gatilho);

        Assert.Equal(1, await CriarServico().ConcluirViagensTerminadasAsync(Agora, default));
        Assert.Equal(nameof(EstadoViagem.Ativa), await EstadoAsync(comFalha.Id));
        Assert.Equal(nameof(EstadoViagem.Concluida), await EstadoAsync(normal.Id));
    }

    [Theory]
    [InlineData(null, 60)]
    [InlineData("300", 300)]
    [InlineData("0", 1)]
    public void I110_16_Temporizador_IntervaloConfiguravel(string? valor, int segundosEsperados)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConclusaoViagens:IntervaloSegundos"] = valor })
            .Build();

        var trabalhador = new TrabalhadorConclusaoViagens(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            configuracao,
            NullLogger<TrabalhadorConclusaoViagens>.Instance);

        Assert.Equal(TimeSpan.FromSeconds(segundosEsperados), trabalhador.Intervalo);
    }
}
