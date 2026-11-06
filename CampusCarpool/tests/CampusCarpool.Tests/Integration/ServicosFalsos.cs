using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comunicacao;
using CampusCarpool.Application.Historico;
using CampusCarpool.Application.Identidade;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Participacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CampusCarpool.Tests;

/// <summary>Serviço de push que não faz nada (para testes que não verificam o push).</summary>
public sealed class ServicoPushFalso : IServicoPush
{
    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Notificador em tempo real falso: regista os eventos para o teste os verificar.</summary>
public sealed class NotificadorPedidosFalso : INotificadorPedidosTempoReal
{
    public Task PedidoRecebidoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoAceiteAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoRejeitadoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Serviço de push falso que guarda cada envio, para verificar destinatários e conteúdo.</summary>
public sealed class ServicoPushRegistador : IServicoPush
{
    public List<(Guid UtilizadorId, string Titulo,
        string Mensagem, string Tipo)> Envios
    { get; } = new();

    public bool SimularFalha { get; set; }

    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
    {
        if (SimularFalha)
            throw new HttpRequestException("Falha simulada no serviço push.");

        Envios.Add((utilizadorId, titulo, mensagem, tipo));

        return Task.CompletedTask;
    }
}

/// <summary>Rota falsa, para testes que não verificam a rota. Com <see cref="Disponivel"/> a falso simula o Serviço de Mapas em baixo.</summary>
public sealed class RotaViagemFalsa : IRotaService
{
    public bool Disponivel { get; set; } = true;

    public Task<ResultadoRotaComPontos?> CalcularAsync(
        Viagem viagem,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct)
        => Task.FromResult<ResultadoRotaComPontos?>(null);

    public Task<ResultadoRotaComPontos?> CalcularEntreAsync(
        PontoRota origem,
        PontoRota destino,
        IEnumerable<Participacao> participacoes,
        CancellationToken ct)
        => Task.FromResult<ResultadoRotaComPontos?>(null);

    public Task<bool> RecalcularAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
        => Task.FromResult(Disponivel);

    public Task<ResultadoRotaComPontos?> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
        => Task.FromResult(RotaGuardada.Ler(viagem));
}

/// <summary>Unidade de trabalho falsa, que conta as gravações. Com <see cref="Falha"/> simula um conflito de concorrência.</summary>
public sealed class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int Confirmacoes { get; private set; }
    public Exception? Falha { get; set; }

    public Task ConfirmarAsync(CancellationToken ct)
    {
        if (Falha is not null) throw Falha;
        Confirmacoes++;
        return Task.CompletedTask;
    }


    public void DescartarAlteracoes()
    {
    }
}

/// <summary>Monta os controllers com os serviços reais, como a injeção de dependências faria em execução.</summary>
public static class ServicosDeTeste
{
    public static ConsultaViagensService CriarConsultas(CampusCarpoolDbContext db) => new(
        new RepositorioParticipacoes(db),
        new RepositorioUtilizadores(db),
        new RepositorioAvaliacoes(db));

    public static PesquisaController CriarPesquisaController(CampusCarpoolDbContext db) =>
        new(new PesquisaService(new RepositorioViagens(db), CriarConsultas(db)));

    public static HistoricoService CriarHistorico(CampusCarpoolDbContext db) => new(new RepositorioHistorico(db));

    public static AvisoService CriarAvisos(CampusCarpoolDbContext db) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        new RepositorioAvisos(db),
        CriarNotificacoes(db),
        new UnidadeDeTrabalhoEfCore(db));

    public static EstimativaCustoService CriarCustos(decimal? precoPorKm = 0.10m) =>
        new(Options.Create(new OpcoesCustos { PrecoPorKm = precoPorKm }));

    public static RotaService CriarRotas(CampusCarpoolDbContext db, IServicoMapasRotas mapas) =>
        new(mapas, new UnidadeDeTrabalhoEfCore(db));

    public static ViagemService CriarViagens(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        EstimativaCustoService? custos = null,
        INotificacaoService? notificacaoService = null) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        new RepositorioUtilizadores(db),
        new UnidadeDeTrabalhoEfCore(db),
        mapas,
        CriarRotas(db, mapas),
        CriarConsultas(db),
        custos ?? CriarCustos(),
        notificacaoService ?? CriarNotificacoes(db));

    public static ViagensController CriarViagensController(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        EstimativaCustoService? custos = null,
        INotificacaoService? notificacaoService = null) =>
        new(CriarViagens(db, mapas, custos, notificacaoService));

    public static NotificacaoService CriarNotificacoes(CampusCarpoolDbContext db) =>
        new(new RepositorioNotificacoes(db), new FilaNotificacoesPushFalsa(), new UnidadeDeTrabalhoEfCore(db));

    public static ParticipacaoService CriarParticipacoes(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        INotificacaoService? notificacoes = null,
        INotificadorPedidosTempoReal? tempoReal = null) => new(
        new RepositorioParticipacoes(db),
        new RepositorioViagens(db),
        new UnidadeDeTrabalhoEfCore(db),
        notificacoes ?? CriarNotificacoes(db),
        tempoReal ?? new NotificadorPedidosFalso(),
        CriarRotas(db, mapas),
        mapas,
        new RepositorioUtilizadores(db));

    public static SimulacoesController CriarSimulacoesController(CampusCarpoolDbContext db, IServicoMapasRotas mapas) =>
        new(CriarParticipacoes(db, mapas));

    public static PerfilService CriarPerfil(CampusCarpoolDbContext db) => new(
        new RepositorioUtilizadores(db),
        new RepositorioLigacoesExternas(db),
        new AvaliacaoService(new RepositorioAvaliacoes(db)),
        new UnidadeDeTrabalhoEfCore(db));

    public static ValidadorEmailAcademico CriarValidadorEmail()
    {
        var opcoes = new OpcoesEmailAcademico();
        opcoes.Dominios.Add("estg.ipp.pt");

        return new ValidadorEmailAcademico(Options.Create(opcoes));
    }

    public static AutenticacaoController CriarAutenticacaoController(
        CampusCarpoolDbContext db,
        IConfiguration configuracao,
        IServicoEmail email) =>
        new(new AutenticacaoService(
            new RepositorioUtilizadores(db),
            new RepositorioConfirmacoesConta(db),
            new RepositorioTokensRevogados(db),
            new RepositorioDispositivo(db),
            new UnidadeDeTrabalhoEfCore(db),
            email,
            new HashPalavraPasse(),
            new GeradorTokensJwt(configuracao),
            CriarValidadorEmail()));

    public static RotasController CriarRotasController(
        IServicoMapasRotas mapas,
        EstimativaCustoService custos,
        CampusCarpoolDbContext? db = null) =>
        new(new ConsultaRotasService(
            mapas,
            db is null ? new RotaViagemFalsa() : CriarRotas(db, mapas),
            custos,
            db is null ? null! : CriarConsultas(db),
            new RepositorioViagens(db!),
            new RepositorioParticipacoes(db!)));
}

/// <summary>
/// OpenRouteService simulado: geocodificação, direções e matriz, sem chamadas externas. Conta os pedidos por tipo.
/// </summary>
/// <remarks>
/// Cada texto dá sempre as mesmas coordenadas (por omissão, derivadas do texto; ou as de <see cref="Locais"/>).
/// A matriz devolve <see cref="MetrosPorSegmento"/> e <see cref="SegundosPorSegmento"/> entre pontos diferentes,
/// ou o que <see cref="Duracao"/> indicar para os índices (origem, destino) dos pontos do pedido.
/// </remarks>
public sealed class MapasFalsos : HttpMessageHandler
{
    public int ChamadasGeocodificacao { get; private set; }
    public int ChamadasRotas { get; private set; }
    public bool SimularFalha { get; set; }
    public bool SimularTempoEsgotado { get; set; }
    public double MetrosPorSegmento { get; set; } = 1000;
    public double SegundosPorSegmento { get; set; } = 120;
    public Func<int, int, double>? Duracao { get; set; }
    public Dictionary<string, (double Latitude, double Longitude)> Locais { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cria o cliente de mapas que usa este simulador.</summary>
    public CampusCarpool.Infrastructure.Integrations.ServicoMapasRotas CriarServico(bool configurado = true)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = configurado ? "chave-ficticia-apenas-para-testes" : ""
            })
            .Build();

        return new CampusCarpool.Infrastructure.Integrations.ServicoMapasRotas(new HttpClient(this, disposeHandler: false), configuracao);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (SimularFalha)
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);

        // Como o HttpClient faz quando esgota o Timeout: sem o cancelamento do pedido.
        if (SimularTempoEsgotado)
            throw new TaskCanceledException("Tempo esgotado simulado.");

        var caminho = request.RequestUri!.AbsolutePath;

        if (caminho.Contains("/pelias/v1/search", StringComparison.OrdinalIgnoreCase))
        {
            ChamadasGeocodificacao++;
            var query = Uri.UnescapeDataString(request.RequestUri.Query);
            var inicio = query.IndexOf("text=", StringComparison.Ordinal) + 5;
            var (latitude, longitude) = Coordenadas(query[inicio..].Split('&')[0].Replace('+', ' '));
            return Json(new { features = new[] { new { geometry = new { coordinates = new[] { longitude, latitude } } } } });
        }

        ChamadasRotas++;

        if (caminho.Contains("matrix", StringComparison.OrdinalIgnoreCase))
        {
            using var corpo = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var n = corpo.RootElement.GetProperty("locations").GetArrayLength();
            var duracoes = Enumerable.Range(0, n)
                .Select(i => Enumerable.Range(0, n).Select(j => i == j ? 0 : Duracao?.Invoke(i, j) ?? SegundosPorSegmento).ToArray())
                .ToArray();
            var distancias = Enumerable.Range(0, n)
                .Select(i => Enumerable.Range(0, n).Select(j => i == j ? 0 : MetrosPorSegmento).ToArray())
                .ToArray();
            return Json(new { durations = duracoes, distances = distancias });
        }

        return Json(new
        {
            features = new[]
            {
                new { properties = new { summary = new { distance = MetrosPorSegmento, duration = SegundosPorSegmento } } }
            }
        });
    }

    private (double Latitude, double Longitude) Coordenadas(string texto)
    {
        if (Locais.TryGetValue(texto, out var conhecido)) return conhecido;

        var soma = texto.Aggregate(0, (acumulado, c) => (acumulado * 31 + c) % 10007);
        return (41 + soma / 100000d, -8 - soma / 100000d);
    }

    private static HttpResponseMessage Json(object valor) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(valor),
            System.Text.Encoding.UTF8,
            "application/json")
    };
}
