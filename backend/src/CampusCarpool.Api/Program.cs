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
    .AddJsonOptions(options =>
    {
        // Os enums saem pelo nome, como estão na BD (API-13).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Um campo que o pedido não conhece dá 400, em vez de ser ignorado em silêncio (BE-25, RNF-SEG-06).
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Corpo em falta, mal formado, com um campo a mais ou de tipo errado: a mesma forma e a mesma língua dos
        // outros erros (API-07, BE-25).
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new CorpoErro("Pedido inválido: campo em falta, a mais ou com tipo errado."));
        // Sem ProblemDetails nos erros sem corpo dos controllers (401, 404, 415…): o UseStatusCodePages dá-lhes
        // o corpo { erro } (BE-08).
        options.SuppressMapClientErrors = true;
    });
builder.Services.AddSignalR();
builder.Services.AddPoliticaCors(builder.Configuration);

// Serviços da própria Api: tempo real (SignalR).
builder.Services.AddScoped<
    INotificadorPedidosTempoReal,
    NotificadorPedidosSignalR>();
builder.Services.AddSingleton<
    Microsoft.AspNetCore.SignalR.IUserIdProvider,
    PedidosUserIdProvider>();
// Ligações abertas ao hub por sessão, para o fim de sessão as fechar logo (BE-18).
builder.Services.AddSingleton<LigacoesPorSessao>();
// Bilhetes de uso único para ligar ao hub sem o JWT no endereço (DEF-BE-055).
builder.Services.AddSingleton(new BilhetesHub(TimeProvider.System));
builder.Services.Configure<OpcoesCustos>(builder.Configuration.GetSection(OpcoesCustos.Seccao));
builder.Services.Configure<OpcoesEmailAcademico>(builder.Configuration.GetSection(OpcoesEmailAcademico.Seccao));
builder.Services.AddLimitesPedidos();
// Atrás de um proxy reverso, o limitador conta pelo IP do cliente, só com proxies configurados (BE-20).
builder.Services.AddProxyReverso(builder.Configuration);
// Erros não tratados → GlobalExceptionHandler → { erro } (409 regra de negócio, 500 o resto).
// O AddProblemDetails só existe porque o UseExceptionHandler() o exige ao arrancar; o GlobalExceptionHandler
// escreve sempre a resposta, por isso nenhum ProblemDetails chega ao cliente.
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
        // O WebSocket do browser não envia o cabeçalho Authorization. No caminho do hub, o cliente manda um bilhete
        // de uso único (?bilhete=, pedido em POST /autenticacao/bilhete-hub), que se troca aqui pelo JWT da sessão;
        // o JWT nunca vai no endereço, que fica nos logs de acesso (DEF-BE-055). ?access_token= já não é aceite.
        // Bilhete desconhecido, gasto ou expirado: fica sem token e o pedido recebe 401 sem corpo.
        OnMessageReceived = context =>
        {
            string? bilhete = context.Request.Query["bilhete"];

            if (!string.IsNullOrEmpty(bilhete) && context.Request.Path.StartsWithSegments("/hubs/pedidos"))
            {
                var bilhetes = context.HttpContext.RequestServices.GetRequiredService<BilhetesHub>();
                context.Token = bilhetes.Consumir(bilhete);
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
// Pipeline HTTP, pela ordem em que cada pedido o atravessa.
// Primeiro, o IP e o esquema reais do cliente, se a API estiver atrás de um proxy configurado (BE-20).
if (ProxyReverso.Configurado(app.Configuration)) app.UseForwardedHeaders();
// O tratamento de exceções vem antes dos outros passos, para apanhar os erros de todos os seguintes.
app.UseExceptionHandler();
// Erros sem corpo (404 de rota, 405, 415, 403) passam a ter { erro }; o 401 fica sem corpo (BE-08).
app.UseStatusCodePages(RespostasSemCorpo.EscreverErroAsync);
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
