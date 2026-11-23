using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comum;
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
    public Task EnviarAsync(NotificacaoPushPendente notificacao, CancellationToken ct)
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

    public Task EnviarAsync(NotificacaoPushPendente notificacao, CancellationToken ct)
    {
        if (SimularFalha)
            throw new HttpRequestException("Falha simulada no serviço push.");

        Envios.Add((notificacao.UtilizadorId, notificacao.Titulo, notificacao.Mensagem, notificacao.Tipo));

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

    public Task<Resultado<ResultadoRotaComPontos>> ObterAsync(
        Viagem viagem,
        IReadOnlyCollection<Participacao> aceites,
        CancellationToken ct)
    {
        ResultadoRotaComPontos? guardada = RotaGuardada.Ler(viagem);

        Resultado<ResultadoRotaComPontos> resultado = guardada is null
            ? new Erro(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.")
            : guardada;

        return Task.FromResult(resultado);
    }

    public Task<ResultadoRotaComPontos> GuardarAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
    {
        RotaGuardada.Guardar(viagem, rota);
        return Task.FromResult(rota);
    }

    public Task<ResultadoRotaComPontos> JuntarLinhaAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
        => Task.FromResult(rota);

    public Task<ResultadoRotaComPontos> ConcluirSimulacaoAsync(Viagem viagem, ResultadoRotaComPontos rota, CancellationToken ct)
        => Task.FromResult(rota);

    public Task<IReadOnlyList<Coordenadas>?> PedirLinhaAsync(IReadOnlyList<Coordenadas> pontos, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Coordenadas>?>(null);
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

    public async Task<Exception?> TentarConfirmarAsync(CancellationToken ct)
    {
        await ConfirmarAsync(ct);
        return null;
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

    /// <summary>Serviço de rotas real; sem <paramref name="geometria"/>, a HERE fica por configurar (linhas retas).</summary>
    public static RotaService CriarRotas(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        IServicoGeometriaRotas? geometria = null) =>
        new(
            mapas,
            geometria ?? new HereFalsa().CriarServico(configurado: false),
            new UnidadeDeTrabalhoEfCore(db),
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

    /// <summary>Viagens: criar, consultar, listar e editar. Passar o mesmo <paramref name="rotas"/> da simulação para partilhar a memória das simulações (D-2).</summary>
    public static ViagemService CriarViagens(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        EstimativaCustoService? custos = null,
        INotificacaoService? notificacaoService = null,
        IServicoGeometriaRotas? geometria = null,
        IRotaService? rotas = null) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        new UnidadeDeTrabalhoEfCore(db),
        mapas,
        rotas ?? CriarRotas(db, mapas, geometria),
        CriarConsultas(db),
        custos ?? CriarCustos(),
        notificacaoService ?? CriarNotificacoes(db));

    public static ViagensController CriarViagensController(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        EstimativaCustoService? custos = null,
        INotificacaoService? notificacaoService = null)
    {
        // O mesmo serviço de rotas no PUT e na simulação, como no contentor: partilham a memória das simulações (D-2).
        RotaService rotas = CriarRotas(db, mapas);

        return new(
            CriarViagens(db, mapas, custos, notificacaoService, rotas: rotas),
            CriarSimulacoesDeAlteracao(db, mapas, custos, rotas: rotas),
            CriarCicloDeVida(db, notificacaoService));
    }

    /// <summary>Simulação da alteração da viagem (AE-01). Passar o mesmo <paramref name="rotas"/> do PUT para partilhar a memória das simulações (D-2).</summary>
    public static SimulacaoDeAlteracaoService CriarSimulacoesDeAlteracao(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        EstimativaCustoService? custos = null,
        IServicoGeometriaRotas? geometria = null,
        IRotaService? rotas = null) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        mapas,
        rotas ?? CriarRotas(db, mapas, geometria),
        custos ?? CriarCustos());

    /// <summary>Indisponibilizar e concluir uma viagem (AE-02).</summary>
    public static CicloDeVidaDaViagemService CriarCicloDeVida(
        CampusCarpoolDbContext db,
        INotificacaoService? notificacaoService = null) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        new UnidadeDeTrabalhoEfCore(db),
        notificacaoService ?? CriarNotificacoes(db));

    public static NotificacaoService CriarNotificacoes(CampusCarpoolDbContext db) =>
        new(new RepositorioNotificacoes(db), new FilaNotificacoesPushFalsa(), new UnidadeDeTrabalhoEfCore(db));

    public static ParticipacaoService CriarParticipacoes(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        INotificacaoService? notificacoes = null,
        INotificadorPedidosTempoReal? tempoReal = null,
        IServicoGeometriaRotas? geometria = null,
        IRotaService? rotas = null) => new(
        new RepositorioParticipacoes(db),
        new RepositorioViagens(db),
        new UnidadeDeTrabalhoEfCore(db),
        notificacoes ?? CriarNotificacoes(db),
        tempoReal ?? new NotificadorPedidosFalso(),
        rotas ?? CriarRotas(db, mapas, geometria),
        mapas);

    public static SimulacoesController CriarSimulacoesController(CampusCarpoolDbContext db, IServicoMapasRotas mapas) =>
        new(CriarSimulacoesDePedido(db, mapas));

    /// <summary>Simulação do pedido real (AE-01). Passar o mesmo <paramref name="rotas"/> do aceitar para partilhar a memória das simulações (D-2).</summary>
    public static SimulacaoDePedidoService CriarSimulacoesDePedido(
        CampusCarpoolDbContext db,
        IServicoMapasRotas mapas,
        IServicoGeometriaRotas? geometria = null,
        IRotaService? rotas = null) => new(
        new RepositorioViagens(db),
        new RepositorioParticipacoes(db),
        rotas ?? CriarRotas(db, mapas, geometria),
        mapas,
        CriarCustos());

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
        new(CriarAutenticacaoService(db, configuracao, email), new LigacoesPorSessao(), new BilhetesHub(TimeProvider.System));

    public static AutenticacaoService CriarAutenticacaoService(
        CampusCarpoolDbContext db,
        IConfiguration configuracao,
        IServicoEmail email) =>
        new(
            new RepositorioUtilizadores(db),
            new RepositorioConfirmacoesConta(db),
            new RepositorioTokensRevogados(db),
            new RepositorioDispositivo(db),
            new UnidadeDeTrabalhoEfCore(db),
            new FilaEmailsDireta(email),
            new HashPalavraPasse(),
            new GeradorTokensJwt(configuracao),
            CriarValidadorEmail());

    public static RotasController CriarRotasController(
        IServicoMapasRotas mapas,
        EstimativaCustoService custos,
        CampusCarpoolDbContext? db = null,
        IServicoGeometriaRotas? geometria = null) =>
        new(new ConsultaRotasService(
            mapas,
            db is null ? new RotaViagemFalsa() : CriarRotas(db, mapas, geometria),
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

    /// <summary>Executado em cada geocodificação, para mudar a BD enquanto um pedido espera pelos mapas.</summary>
    public Action? AntesDeGeocodificar { get; set; }
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

    /// <summary>Tempo que cada pedido demora a responder (para o limite de 10 s por operação, BE-11).</summary>
    public TimeSpan Atraso { get; set; } = TimeSpan.Zero;

    /// <summary>Todos os pedidos recebidos, incluindo os que respondem com falha (os contadores por tipo só contam os com sucesso).</summary>
    public int PedidosRecebidos { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        PedidosRecebidos++;

        if (Atraso > TimeSpan.Zero)
            await Task.Delay(Atraso, cancellationToken);

        if (SimularFalha)
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);

        // Como o HttpClient faz quando esgota o Timeout: sem o cancelamento do pedido.
        if (SimularTempoEsgotado)
            throw new TaskCanceledException("Tempo esgotado simulado.");

        var caminho = request.RequestUri!.AbsolutePath;

        if (caminho.Contains("/pelias/v1/search", StringComparison.OrdinalIgnoreCase))
        {
            ChamadasGeocodificacao++;
            AntesDeGeocodificar?.Invoke();
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

/// <summary>
/// HERE Routing v8 simulado: responde a cada pedido com uma rota de uma secção e uma linha fixa (<see cref="Polilinha"/>),
/// ou com o código de <see cref="Falha"/>. Guarda o último URL pedido e conta os pedidos.
/// </summary>
public sealed class HereFalsa : HttpMessageHandler
{
    /// <summary>Exemplo da documentação da HERE (precisão 5): 4 pontos perto de Frankfurt.</summary>
    public const string Polilinha = "BFoz5xJ67i1B1B7PzIhaxL7Y";

    public int Chamadas { get; private set; }
    public Uri? UltimoPedido { get; private set; }
    public System.Net.HttpStatusCode? Falha { get; set; }

    /// <summary>Cria o cliente da HERE que usa este simulador; sem chave, nunca chega a fazer pedidos.</summary>
    public CampusCarpool.Infrastructure.Integrations.ServicoGeometriaHere CriarServico(bool configurado = true)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Here:ApiKey"] = configurado ? "chave-here-ficticia-apenas-para-testes" : ""
            })
            .Build();

        return new CampusCarpool.Infrastructure.Integrations.ServicoGeometriaHere(
            new HttpClient(this, disposeHandler: false),
            configuracao,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CampusCarpool.Infrastructure.Integrations.ServicoGeometriaHere>.Instance);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Chamadas++;
        UltimoPedido = request.RequestUri;

        if (Falha is System.Net.HttpStatusCode codigo)
            return Task.FromResult(new HttpResponseMessage(codigo));

        string json = "{\"routes\":[{\"sections\":[{\"polyline\":\"" + Polilinha + "\"}]}]}";

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }
}

/// <summary>
/// HERE simulada para o Serviço de Mapas principal (D-5): geocodificação, rota direta e matriz, conforme o anfitrião
/// do pedido. Conta os pedidos por tipo e guarda o último URL e o último corpo enviado.
/// </summary>
/// <remarks>
/// Cada texto dá as coordenadas de <see cref="Locais"/> (ou uma lista vazia, se não estiver lá). A rota direta tem
/// 12,3 km, 15 min e a linha de <see cref="HereFalsa.Polilinha"/> (4 pontos). A matriz dá 1000 m e 120 s entre pontos
/// diferentes; os pares de <see cref="ParesSemRota"/> vêm com <c>errorCodes</c> 1. Com <see cref="Falha"/> responde
/// esse código; com <see cref="SimularTempoEsgotado"/> lança como o HttpClient quando esgota o seu tempo.
/// </remarks>
public sealed class HereMapasFalsa : HttpMessageHandler
{
    public int ChamadasGeocodificacao { get; private set; }
    public int ChamadasRota { get; private set; }
    public int ChamadasMatriz { get; private set; }
    public Uri? UltimoPedido { get; private set; }
    public string? UltimoCorpo { get; private set; }
    public System.Net.HttpStatusCode? Falha { get; set; }
    public bool SimularTempoEsgotado { get; set; }
    public Dictionary<string, (double Latitude, double Longitude)> Locais { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<(int, int)> ParesSemRota { get; } = [];
    /// <summary>Corpo da resposta da rota direta, em vez do de omissão (ex.: <c>{"routes":[]}</c>, sem rota).</summary>
    public string? RespostaRota { get; set; }

    /// <summary>Cria o cliente da HERE que usa este simulador.</summary>
    public CampusCarpool.Infrastructure.Integrations.ServicoMapasHere CriarServico(bool configurado = true)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Here:ApiKey"] = configurado ? "chave-here-ficticia-apenas-para-testes" : ""
            })
            .Build();

        return new CampusCarpool.Infrastructure.Integrations.ServicoMapasHere(new HttpClient(this, disposeHandler: false), configuracao);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UltimoPedido = request.RequestUri;
        UltimoCorpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        string anfitriao = request.RequestUri!.Host;

        if (anfitriao.StartsWith("geocode", StringComparison.Ordinal)) ChamadasGeocodificacao++;
        else if (anfitriao.StartsWith("matrix", StringComparison.Ordinal)) ChamadasMatriz++;
        else ChamadasRota++;

        if (SimularTempoEsgotado)
            throw new TaskCanceledException("Tempo esgotado simulado.");

        if (Falha is System.Net.HttpStatusCode codigo)
            return new HttpResponseMessage(codigo);

        if (anfitriao.StartsWith("geocode", StringComparison.Ordinal))
            return Geocodificacao(request.RequestUri);

        if (anfitriao.StartsWith("matrix", StringComparison.Ordinal))
            return Matriz(UltimoCorpo!);

        if (RespostaRota is not null)
            return Json(RespostaRota);

        return Json("{\"routes\":[{\"sections\":[{\"summary\":{\"length\":12300,\"duration\":900},\"polyline\":\"" + HereFalsa.Polilinha + "\"}]}]}");
    }

    private HttpResponseMessage Geocodificacao(Uri endereco)
    {
        string consulta = Uri.UnescapeDataString(endereco.Query);
        int inicio = consulta.IndexOf("q=", StringComparison.Ordinal) + 2;
        string texto = consulta[inicio..].Split('&')[0];

        if (!Locais.TryGetValue(texto, out var ponto))
            return Json("{\"items\":[]}");

        string lat = ponto.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string lng = ponto.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Json("{\"items\":[{\"title\":\"" + texto + "\",\"position\":{\"lat\":" + lat + ",\"lng\":" + lng + "}}]}");
    }

    private HttpResponseMessage Matriz(string corpo)
    {
        using var json = System.Text.Json.JsonDocument.Parse(corpo);
        int n = json.RootElement.GetProperty("origins").GetArrayLength();
        var tempos = new List<int>();
        var distancias = new List<int>();
        var erros = new List<int>();

        // Lista plana, linha a linha: o par (i, j) fica na posição n × i + j.
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                tempos.Add(i == j ? 0 : 120);
                distancias.Add(i == j ? 0 : 1000);
                erros.Add(ParesSemRota.Contains((i, j)) ? 1 : 0);
            }
        }

        return Json("{\"matrixId\":\"m\",\"matrix\":{\"numOrigins\":" + n + ",\"numDestinations\":" + n
            + ",\"travelTimes\":[" + string.Join(",", tempos) + "],\"distances\":[" + string.Join(",", distancias)
            + "],\"errorCodes\":[" + string.Join(",", erros) + "]},\"regionDefinition\":{\"type\":\"circle\"}}");
    }

    private static HttpResponseMessage Json(string json) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };
}
