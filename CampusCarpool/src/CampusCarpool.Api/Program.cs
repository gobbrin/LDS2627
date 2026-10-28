using System.Text;
using CampusCarpool.Api.Hubs;
using CampusCarpool.Api.Services;
using CampusCarpool.Api.Middleware;
using CampusCarpool.Application.DependencyInjection;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Infrastructure.DependencyInjection;
using CampusCarpool.Infrastructure.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddScoped<
    INotificadorPedidosTempoReal,
    NotificadorPedidosSignalR>();
builder.Services.AddSingleton<
    Microsoft.AspNetCore.SignalR.IUserIdProvider,
    PedidosUserIdProvider>();
builder.Services.AddScoped<EstimativaCustoService>();
builder.Services.AddScoped<ConsultaRotaViagemService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IServicoEmail, ServicoEmail>();
builder.Services.AddHttpClient<IServicoPush, ServicoPush>();

builder.Services.AddHttpClient<MapasService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(
        (config["Maps:BaseUrl"] ?? "https://api.openrouteservice.org")
        .TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(
        config.GetValue("Maps:TimeoutSeconds", 10));
});
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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// I049 - Historico pessoal
builder.Services.AddScoped<
    CampusCarpool.Application.Interfaces.IHistoricoService,
    CampusCarpool.Infrastructure.Services.HistoricoService>();


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
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
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
        }
    };
});

builder.Services.AddAuthorization();
var app = builder.Build();
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
public partial class Program { }
