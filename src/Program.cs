using FIAP.MicroService.Payments.Application.Services;
using FIAP.MicroService.Payments.Application.Settings;
using FIAP.MicroService.Payments.Data;
using FIAP.MicroService.Payments.Data.Repositories;
using FIAP.MicroService.Payments.Domain.Repositories;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("A string de conexão não foi encontrada!");

builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura Forwarded Headers (p/ respeitar prefixos e host do GW)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();

builder
    .Services
    .AddOptions<GameApiSettings>()
    .BindConfiguration(nameof(GameApiSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder
    .Services
    .AddOptions<UserApiSettings>()
    .BindConfiguration(nameof(UserApiSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder
    .Services
    .AddOptions<ServiceBusSettings>()
    .BindConfiguration(nameof(ServiceBusSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();


builder.Services.AddHttpClient<IGameApiService, GameApiService>((sp, client) => {
    var settings = sp.GetRequiredService<IOptions<GameApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHttpClient<IUserApiService, UserApiService>((sp, client) => {
    var settings = sp.GetRequiredService<IOptions<UserApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

#region Health Checks

// ------------------------------
// Configuração de Health Checks para Kubernetes
// ------------------------------

builder.Services.AddHealthChecks()
    // Verifica conexão com SQL Server
    .AddSqlServer(
        connectionString: connectionString,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "ready" })
    // Verifica se a aplicação está respondendo (liveness básico)
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"),
        tags: new[] { "live" });

#endregion

var app = builder.Build();

app.UseForwardedHeaders();

// aplica X-Forwarded-Prefix como PathBase
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefix) &&
        !string.IsNullOrWhiteSpace(prefix))
    {
        var calculatedPrefix = prefix.ToString().Split(',', 2)[0].Trim();
        if (!calculatedPrefix.StartsWith("/")) calculatedPrefix = "/" + calculatedPrefix;
        ctx.Request.PathBase = calculatedPrefix.TrimEnd('/');
    }
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#region Health Check Endpoints

// ------------------------------
// Endpoints de Health Check
// ------------------------------

// Endpoint principal - verifica tudo (para readinessProbe do K8s)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthCheckResponse
});

// Endpoint de liveness - verifica apenas se a API está viva (para livenessProbe do K8s)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthCheckResponse
});

// Endpoint de readiness - verifica dependências (para readinessProbe do K8s)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

// Função para formatar resposta JSON dos health checks
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            exception = e.Value.Exception?.Message,
            tags = e.Value.Tags
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
}

#endregion

app.Run();