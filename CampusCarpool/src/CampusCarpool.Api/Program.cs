using System.Text;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Api.Middleware;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.DependencyInjection;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.DependencyInjection;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Ponto de entrada da API (top-level statements): regista os serviços (DI) e monta o pipeline HTTP.
// Configuração obrigatória para arrancar: Jwt:Key (>= 32 bytes) e ConnectionStrings:DefaultConnection, por
// User Secrets em desenvolvimento ou variáveis de ambiente (Jwt__Key, ConnectionStrings__DefaultConnection).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Serviços da própria Api: tempo real (SignalR), custos e rotas.
builder.Services.AddScoped<
    INotificadorPedidosTempoReal,
    NotificadorPedidosSignalR>();
builder.Services.AddSingleton<
    Microsoft.AspNetCore.SignalR.IUserIdProvider,
    PedidosUserIdProvider>();
builder.Services.AddScoped<EstimativaCustoService>();
builder.Services.AddScoped<ConsultaRotaViagemService>();
// Integrações externas com HttpClient gerido pela fábrica (Brevo, FCM, OpenRouteService).
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IServicoEmail, ServicoEmail>();
builder.Services.AddHttpClient<IServicoPush, ServicoPush>();

// O BaseAddress não tem efeito: o MapasService usa URLs absolutos (IN-04). O timeout (10 s) aplica-se a cada pedido.
builder.Services.AddHttpClient<MapasService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(
        config["Maps:BaseUrl"] ?? "https://api.heigit.org/"
        .TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(
        config.GetValue("Maps:TimeoutSeconds", 10));
});
// Limitador global: 60 pedidos por minuto por IP (janela fixa, sem fila), 429 acima disso; vale também para o
// hub e /health. Utilizadores atrás do mesmo NAT (rede do campus, rede móvel) partilham o contador (API-04 / G19).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
// Erros não tratados → GlobalExceptionHandler → ProblemDetails (409 regra de negócio, 500 o resto).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
// /health só indica que o processo responde; não verifica a BD (C1).
builder.Services.AddHealthChecks();
// Camadas Application e Infrastructure (esta lança exceção sem ConnectionStrings:DefaultConnection).
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// I049 - Historico pessoal
builder.Services.AddScoped<
    CampusCarpool.Application.Interfaces.IHistoricoService,
    CampusCarpool.Infrastructure.Services.HistoricoService>();


// Autenticação JWT Bearer. A chave não está no appsettings.json (I110, API-01): sem ela a API não arranca.
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Definir Jwt:Key com pelo menos 32 bytes através de User Secrets ou variáveis de ambiente.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        // P-17: tolerância de relógio de 30 s (o valor por omissão do .NET seria 5 min).
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        // O WebSocket do SignalR não envia o cabeçalho Authorization: aceita o token na query string,
        // mas só no caminho do hub (DC-46: fica nos logs de acesso).
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var requestPath = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                requestPath.StartsWithSegments("/hubs/pedidos"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        // Fim de sessão (I110, DEF-BE-004): recusa os tokens revogados.
        OnTokenValidated = ValidacaoTokensRevogados.ValidarAsync
    };
});

// Sem FallbackPolicy: cada controller tem de pôr [Authorize] (API-10). Sem CORS (DC-45, fica para m4).
builder.Services.AddAuthorization();
var app = builder.Build();
// Pipeline HTTP, pela ordem em que cada pedido o atravessa. O tratamento de exceções vem primeiro para
// apanhar os erros de todos os passos seguintes.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PedidosHub>("/hubs/pedidos");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "Campus Carpool API", status = "running" }));
app.Run();
// Torna a classe Program pública para os testes com WebApplicationFactory<Program>.
public partial class Program { }
