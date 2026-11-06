using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CampusCarpool.Tests.Integration;

/// <summary>Email falso: guarda os tokens de confirmação, para o teste percorrer o registo até ao fim.</summary>
public sealed class EmailRegistador : IServicoEmail
{
    private readonly ConcurrentQueue<string> _tokens = new();

    public IReadOnlyCollection<string> Tokens => _tokens;

    public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
    {
        _tokens.Enqueue(token);
        return Task.CompletedTask;
    }
}

/// <summary>Guarda o que a aplicação escreve nos logs, depois dos filtros do <c>appsettings.json</c>.</summary>
public sealed class RegistadorDeLogs(ConcurrentQueue<string> destino) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Registador(categoryName, destino);

    public void Dispose()
    {
    }

    private sealed class Registador(string categoria, ConcurrentQueue<string> destino) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            destino.Enqueue($"{logLevel} {categoria}: {formatter(state, exception)} {exception}");
    }
}

/// <summary>A API tal como o <c>Program.cs</c> a monta: JwtBearer, limitador, tratamento de erros, controllers e hub (TE-04).</summary>
/// <remarks>
/// Só se troca o que sai do processo: a base de dados (ver <see cref="BaseDeDadosDeTeste"/>), o Serviço de Mapas (<see cref="MapasFalsos"/>), o email (<see cref="EmailRegistador"/>) e a fila de push
/// (<see cref="FilaNotificacoesPushFalsa"/>). Cada pedido usa a sua ligação à mesma BD, porque o hub e os pedidos correm
/// em paralelo. Corre como <c>Production</c>, sem User Secrets nem OpenAPI. Os limites de pedidos estão ativos: um teste
/// que faça muitos pedidos por minuto recebe 429, por isso cada teste tem a sua instância.
/// </remarks>
public sealed class AplicacaoReal(Action<IServiceCollection>? substituicoes = null) : WebApplicationFactory<Program>
{
    public const string ChaveJwt = "CHAVE_DE_TESTES_I110_COM_MAIS_DE_32_BYTES_12345";
    public const string Emissor = "CampusCarpool";
    public const string Audiencia = "CampusCarpool.Clients";
    public const string PalavraPasse = "Palavra-passe-de-teste-1";

    private readonly string _cadeiaDeLigacao = $"Data Source=aplicacao-real-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _ligacaoPermanente;

    public MapasFalsos Mapas { get; } = new();
    public EmailRegistador Email { get; } = new();
    public FilaNotificacoesPushFalsa FilaPush { get; } = new();
    public ConcurrentQueue<string> Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(local);Database=CampusCarpool_Tests;");
        builder.UseSetting("Jwt:Key", ChaveJwt);

        builder.ConfigureLogging(logging => logging.AddProvider(new RegistadorDeLogs(Logs)));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<CampusCarpoolDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CampusCarpoolDbContext>>();
            services.AddDbContext<CampusCarpoolDbContext>(options =>
            {
                if (BaseDeDadosDeTeste.EmSqlServer) options.UseBaseDeTeste(_ligacaoPermanente!);
                else options.UseSqlite(_cadeiaDeLigacao);
            });

            services.RemoveAll<IServicoMapasRotas>();
            services.AddSingleton<IServicoMapasRotas>(Mapas.CriarServico());
            services.RemoveAll<IServicoEmail>();
            services.AddSingleton<IServicoEmail>(Email);
            services.RemoveAll<IFilaNotificacoesPush>();
            services.AddSingleton<IFilaNotificacoesPush>(FilaPush);

            substituicoes?.Invoke(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // A BD em memória existe enquanto houver uma ligação aberta.
        _ligacaoPermanente = new SqliteConnection(_cadeiaDeLigacao);
        _ligacaoPermanente.Open();

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>().Database.CriarEsquemaDeTeste();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ligacaoPermanente?.Dispose();
    }

    /// <summary>Executa uma consulta ou alteração direta à BD, fora da API.</summary>
    public T NaBaseDeDados<T>(Func<CampusCarpoolDbContext, T> operacao)
    {
        using var scope = Services.CreateScope();
        return operacao(scope.ServiceProvider.GetRequiredService<CampusCarpoolDbContext>());
    }

    /// <summary>Grava um utilizador com a palavra-passe <see cref="PalavraPasse"/>.</summary>
    public Utilizador CriarUtilizador(string nome, bool confirmado = true)
    {
        var utilizador = new Utilizador
        {
            Nome = nome,
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = confirmado
        };
        utilizador.PasswordHash = new PasswordHasher<Utilizador>().HashPassword(utilizador, PalavraPasse);

        return NaBaseDeDados(db =>
        {
            db.Utilizadores.Add(utilizador);
            db.SaveChanges();
            return utilizador;
        });
    }

    /// <summary>Token emitido pelo gerador real da API.</summary>
    public string TokenDe(Utilizador utilizador) =>
        Services.GetRequiredService<IGeradorTokens>().Gerar(utilizador.Id).AccessToken;

    /// <summary>Cliente HTTP com o token real do utilizador.</summary>
    public HttpClient ClienteDe(Utilizador utilizador)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenDe(utilizador));
        return cliente;
    }

    /// <summary>Cria um token à mão, para os casos que o gerador da API nunca produz.</summary>
    /// <param name="utilizadorId">Utilizador da claim <c>sub</c>.</param>
    /// <param name="expiraEm">Fim da validade, em UTC.</param>
    /// <param name="chave">Chave de assinatura.</param>
    /// <param name="emissor">Emissor.</param>
    /// <param name="audiencia">Audiência.</param>
    /// <param name="comJti">Se inclui a claim <c>jti</c>.</param>
    public static string TokenManual(
        Guid utilizadorId,
        DateTime expiraEm,
        string chave = ChaveJwt,
        string emissor = Emissor,
        string audiencia = Audiencia,
        bool comJti = true)
    {
        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, utilizadorId.ToString())];
        if (comJti) claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var jwt = new JwtSecurityToken(
            emissor,
            audiencia,
            claims,
            notBefore: expiraEm.AddMinutes(-60),
            expires: expiraEm,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    /// <summary>Cria uma viagem ativa pela API, para amanhã, e devolve o id.</summary>
    public static async Task<Guid> CriarViagemAsync(HttpClient condutor, int lugares = 3)
    {
        using var resposta = await condutor.PostAsJsonAsync(
            "/api/v1/viagens",
            new { origem = "Porto", destino = "Braga", partidaEm = DateTime.UtcNow.AddDays(1), lugares });

        return await LerIdAsync(resposta);
    }

    /// <summary>Submete um pedido pela API e devolve o id da participação.</summary>
    public static async Task<Guid> PedirParticipacaoAsync(HttpClient passageiro, Guid viagemId, string recolha = "Maia")
    {
        using var resposta = await passageiro.PostAsJsonAsync(
            "/api/v1/pedidos",
            new { viagemId, recolha, destino = (string?)null });

        return await LerIdAsync(resposta);
    }

    /// <summary>Aceita um pedido pela API.</summary>
    public static async Task AceitarAsync(HttpClient condutor, Guid participacaoId)
    {
        using var resposta = await condutor.PostAsync($"/api/v1/participacoes/{participacaoId}/aceitar", null);
        resposta.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> LerIdAsync(HttpResponseMessage resposta)
    {
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }
}
