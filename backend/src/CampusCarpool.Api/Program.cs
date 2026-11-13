using System.Text;
using System.Text.Json.Serialization;
using CampusCarpool.Api.Configuration;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Api.Middleware;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.DependencyInjection;
using CampusCarpool.Application.Identidade;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

// Ponto de entrada da API (top-level statements): regista os serviços (DI) e monta o pipeline HTTP.
// Configuração obrigatória para arrancar: Jwt:Key (>= 32 bytes) e ConnectionStrings:DefaultConnection, por
// User Secrets em desenvolvimento ou variáveis de ambiente (Jwt__Key, ConnectionStrings__DefaultConnection).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Os enums saem pelo nome, como estão na BD (API-13).
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // Corpo em falta ou mal formado: a mesma forma e a mesma língua dos outros erros (API-07).
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new CorpoErro("Pedido inválido. Confirma se todos os campos estão preenchidos.")));
builder.Services.AddSignalR();
builder.Services.AddPoliticaCors(builder.Configuration);

// Serviços da própria Api: tempo real (SignalR).
builder.Services.AddScoped<
    INotificadorPedidosTempoReal,
    NotificadorPedidosSignalR>();
builder.Services.AddSingleton<
    Microsoft.AspNetCore.SignalR.IUserIdProvider,
    PedidosUserIdProvider>();
builder.Services.Configure<OpcoesCustos>(builder.Configuration.GetSection(OpcoesCustos.Seccao));
builder.Services.Configure<OpcoesEmailAcademico>(builder.Configuration.GetSection(OpcoesEmailAcademico.Seccao));
builder.Services.AddLimitesPedidos();
// Erros não tratados → GlobalExceptionHandler → ProblemDetails (409 regra de negócio, 500 o resto).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
// Camadas Application e Infrastructure (esta lança exceção sem ConnectionStrings:DefaultConnection).
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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

// RNF-SEG-04: uma rota nova exige sessão por omissão; as públicas têm de o declarar com [AllowAnonymous] (API-10).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
var app = builder.Build();
// Pipeline HTTP, pela ordem em que cada pedido o atravessa. O tratamento de exceções vem primeiro para
// apanhar os erros de todos os passos seguintes.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();
else app.UseHsts();
// Antes do redirecionamento: um pedido de verificação do browser (OPTIONS) não pode ser redirecionado.
app.UseCors();
app.UseHttpsRedirection();
// Depois da autenticação, para o limite global contar por utilizador (A6).
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
// A ligação fecha quando o token expira, em vez de continuar a receber eventos (API-11).
app.MapHub<PedidosHub>("/hubs/pedidos", options => options.CloseOnAuthenticationExpiration = true);
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { service = "Campus Carpool API", status = "running" })).AllowAnonymous();
app.Run();
// Torna a classe Program pública para os testes com WebApplicationFactory<Program>.
public partial class Program { }
