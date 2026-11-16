using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Simulação da alteração de origem ou destino (<c>ViagemService.SimularAlteracaoAsync</c>, WEB-01, MOB-01): o mesmo
/// cálculo do PUT, com as paragens dos passageiros aceites e o custo, sem gravar nada e sem chamar a HERE.
/// </summary>
/// <remarks>
/// Testes do serviço, chamado diretamente, como os outros testes do <c>ViagemService</c>: SQLite em memória com os
/// serviços reais; OpenRouteService pelo <see cref="MapasFalsos"/> (1 km e 2 min por troço) e HERE pela <see cref="HereFalsa"/>.
/// </remarks>
public sealed class SimularAlteracaoViagemTests : IDisposable
{
    private readonly SqliteConnection ligacao = new("Data Source=:memory:");
    private readonly MapasFalsos mapas = new();
    private readonly HereFalsa here = new();
    private readonly CampusCarpoolDbContext db;
    private readonly Utilizador condutor = new() { Nome = "Condutor", Email = "condutor@estg.ipp.pt", EmailConfirmado = true };
    private readonly Utilizador passageiro = new() { Nome = "Passageiro", Email = "passageiro@estg.ipp.pt", EmailConfirmado = true };

    public SimularAlteracaoViagemTests()
    {
        ligacao.Open();
        db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(ligacao).Options);
        db.Database.CriarEsquemaDeTeste();
        db.AddRange(condutor, passageiro);
        db.SaveChanges();

        mapas.Locais["Porto"] = (41.15, -8.61);
        mapas.Locais["Braga"] = (41.55, -8.42);
        mapas.Locais["Maia"] = (41.23, -8.62);
        mapas.Locais["Guimarães"] = (41.44, -8.29);
    }

    public void Dispose()
    {
        db.Dispose();
        ligacao.Dispose();
    }

    // Serviço real, com a HERE configurada (para provar que a simulação não a chama).
    private ViagemService Viagens(bool mapasConfigurados = true) =>
        ServicosDeTeste.CriarViagens(db, mapas.CriarServico(mapasConfigurados), geometria: here.CriarServico());

    // Viagem Porto → Braga de 3 lugares, daqui a dois dias; a criação pede a linha à HERE uma vez.
    private async Task<Guid> CriarViagemAsync()
    {
        var criada = await Viagens().CriarAsync(condutor.Id, "Porto", "Braga", DateTime.UtcNow.AddDays(2), 3, default);

        Assert.Null(criada.Erro);
        return criada.Valor.Id;
    }

    // O passageiro pede recolha na Maia (paragem nova) e o condutor aceita.
    private async Task AceitarPassageiroNaMaiaAsync(Guid viagemId)
    {
        var participacoes = ServicosDeTeste.CriarParticipacoes(db, mapas.CriarServico(), geometria: here.CriarServico());
        var pedido = await participacoes.CriarPedidoAsync(viagemId, passageiro.Id, "Maia", null, default);
        Assert.Null(pedido.Erro);

        db.ChangeTracker.Clear();
        Assert.Null((await participacoes.AceitarAsync(pedido.Valor.Id, condutor.Id, default)).Erro);
    }

    // Viagem tal como está na BD, sem passar pela cache do contexto.
    private Viagem ViagemNaBaseDeDados(Guid viagemId)
    {
        db.ChangeTracker.Clear();
        return db.Viagens.AsNoTracking().Single(v => v.Id == viagemId);
    }

    private async Task<Resultado<RotaDaViagem>> SimularAsync(Guid viagemId, Guid utilizadorId, string? origem, string? destino)
    {
        db.ChangeTracker.Clear();
        return await Viagens().SimularAlteracaoAsync(viagemId, utilizadorId, origem, destino, default);
    }

    [Fact]
    /// <summary>Condutor sem passageiros muda o destino: rota direta com o custo só dele, em retas e sem chamar a HERE.</summary>
    public async Task SimularAlteracao_CondutorMudaODestino_DevolveRotaECusto()
    {
        var viagemId = await CriarViagemAsync();
        int chamadasHereDepoisDeCriar = here.Chamadas;

        var simulada = await SimularAsync(viagemId, condutor.Id, null, "Guimarães");

        Assert.Null(simulada.Erro);
        RotaDaViagem rota = simulada.Valor;
        Assert.Equal(viagemId, rota.ViagemId);
        Assert.Equal(["Porto", "Guimarães"], rota.PontosOrdenados.Select(p => p.Local));
        Assert.Equal(1m, rota.DistanciaTotalKm);
        Assert.Equal(2, rota.DuracaoTotalMinutos);
        Assert.Equal(0, rota.EstimativaCusto.PassageirosAceites);
        Assert.Equal(1, rota.EstimativaCusto.TotalParticipantes);

        // Sem a HERE, a linha são retas entre os pontos: origem (Porto) e novo destino (Guimarães).
        Assert.True(rota.GeometriaAproximada);
        Assert.Equal([[41.15m, -8.61m], [41.44m, -8.29m]], rota.Geometria);
        Assert.Equal(chamadasHereDepoisDeCriar, here.Chamadas);
    }

    [Fact]
    /// <summary>Com um passageiro aceite, a rota passa pela recolha dele e o custo divide-se por dois, como no PUT.</summary>
    public async Task SimularAlteracao_ComPassageiroAceite_IncluiAParagemEOCusto()
    {
        var viagemId = await CriarViagemAsync();
        await AceitarPassageiroNaMaiaAsync(viagemId);

        var simulada = await SimularAsync(viagemId, condutor.Id, null, "Guimarães");

        Assert.Null(simulada.Erro);
        RotaDaViagem rota = simulada.Valor;
        Assert.Equal(["Porto", "Maia", "Guimarães"], rota.PontosOrdenados.Select(p => p.Local));
        Assert.Equal(2, rota.Segmentos.Count);
        Assert.Equal(2m, rota.DistanciaTotalKm);
        Assert.Equal(1, rota.ParticipacoesAceites);
        Assert.Equal(1, rota.EstimativaCusto.PassageirosAceites);
        Assert.Equal(2, rota.EstimativaCusto.TotalParticipantes);
        Assert.Equal(3, rota.Geometria.Count);
    }

    [Fact]
    /// <summary>A simulação não grava nada (locais, rota, duração, versão), não notifica nem chama a HERE.</summary>
    public async Task SimularAlteracao_NaoGravaNemNotificaNemChamaAHere()
    {
        var viagemId = await CriarViagemAsync();
        await AceitarPassageiroNaMaiaAsync(viagemId);
        Viagem antes = ViagemNaBaseDeDados(viagemId);
        int notificacoesAntes = db.Notificacoes.Count();
        int chamadasHereAntes = here.Chamadas;

        var simulada = await SimularAsync(viagemId, condutor.Id, null, "Guimarães");

        Assert.Null(simulada.Erro);
        Viagem depois = ViagemNaBaseDeDados(viagemId);
        Assert.Equal(antes.Origem, depois.Origem);
        Assert.Equal(antes.Destino, depois.Destino);
        Assert.Equal(antes.DestinoLatitude, depois.DestinoLatitude);
        Assert.Equal(antes.DuracaoMinutos, depois.DuracaoMinutos);
        Assert.Equal(antes.DistanciaKm, depois.DistanciaKm);
        Assert.Equal(antes.RotaPontos, depois.RotaPontos);
        Assert.Equal(notificacoesAntes, db.Notificacoes.Count());
        Assert.Equal(chamadasHereAntes, here.Chamadas);
    }

    [Fact]
    /// <summary>R-B01: com os mesmos locais devolve a rota guardada (com a linha da HERE), sem chamar os mapas.</summary>
    public async Task SimularAlteracao_MesmosLocais_DevolveARotaGuardadaSemChamarOsMapas()
    {
        var viagemId = await CriarViagemAsync();
        int geocodificacoes = mapas.ChamadasGeocodificacao;
        int rotas = mapas.ChamadasRotas;
        int chamadasHere = here.Chamadas;

        var simulada = await SimularAsync(viagemId, condutor.Id, "Porto", "Braga");

        Assert.Null(simulada.Erro);
        Assert.False(simulada.Valor.GeometriaAproximada);
        Assert.Equal(4, simulada.Valor.Geometria.Count);
        Assert.Equal(geocodificacoes, mapas.ChamadasGeocodificacao);
        Assert.Equal(rotas, mapas.ChamadasRotas);
        Assert.Equal(chamadasHere, here.Chamadas);
    }

    [Fact]
    /// <summary>Só o condutor simula: outro utilizador recebe 403, sem chamar os mapas.</summary>
    public async Task SimularAlteracao_NaoCondutor_Proibido()
    {
        var viagemId = await CriarViagemAsync();
        int geocodificacoes = mapas.ChamadasGeocodificacao;

        var simulada = await SimularAsync(viagemId, passageiro.Id, null, "Guimarães");

        Assert.Equal(TipoErro.Proibido, simulada.Erro?.Tipo);
        Assert.Equal(geocodificacoes, mapas.ChamadasGeocodificacao);
    }

    [Fact]
    /// <summary>Viagem inexistente: 404.</summary>
    public async Task SimularAlteracao_ViagemInexistente_NaoEncontrada()
    {
        var simulada = await SimularAsync(Guid.NewGuid(), condutor.Id, null, "Guimarães");

        Assert.Equal(TipoErro.NaoEncontrado, simulada.Erro?.Tipo);
    }

    [Fact]
    /// <summary>Viagem concluída já não é editável, por isso também não se simula: 409, como no PUT.</summary>
    public async Task SimularAlteracao_ViagemConcluida_Conflito()
    {
        var viagemId = await CriarViagemAsync();
        db.ChangeTracker.Clear();
        db.Viagens.Single(v => v.Id == viagemId).Estado = EstadoViagem.Concluida;
        db.SaveChanges();

        var simulada = await SimularAsync(viagemId, condutor.Id, null, "Guimarães");

        Assert.Equal(TipoErro.Conflito, simulada.Erro?.Tipo);
        Assert.Equal("Viagem não editável.", simulada.Erro?.Mensagem);
    }

    [Fact]
    /// <summary>As validações de texto da edição valem aqui: destino vazio e origem igual ao destino dão 400.</summary>
    public async Task SimularAlteracao_LocaisInvalidos_DadosInvalidos()
    {
        var viagemId = await CriarViagemAsync();

        var vazio = await SimularAsync(viagemId, condutor.Id, null, "   ");
        var mesmoLocal = await SimularAsync(viagemId, condutor.Id, "Braga", "Braga");

        Assert.Equal("Indica o destino da viagem.", vazio.Erro?.Mensagem);
        Assert.Equal(TipoErro.DadosInvalidos, mesmoLocal.Erro?.Tipo);
    }

    [Fact]
    /// <summary>Sem o Serviço de Mapas configurado, mudar um local dá 503, como o resto.</summary>
    public async Task SimularAlteracao_SemMapas_ServicoIndisponivel()
    {
        var viagemId = await CriarViagemAsync();
        db.ChangeTracker.Clear();

        var simulada = await Viagens(mapasConfigurados: false)
            .SimularAlteracaoAsync(viagemId, condutor.Id, null, "Guimarães", default);

        Assert.Equal(TipoErro.ServicoIndisponivel, simulada.Erro?.Tipo);
        Assert.Equal("Serviço de mapas não configurado.", simulada.Erro?.Mensagem);
    }
}
