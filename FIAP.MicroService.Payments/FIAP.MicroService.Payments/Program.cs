using FIAP.MicroService.Payments.Application.Services;
using FIAP.MicroService.Payments.Application.Settings;
using FIAP.MicroService.Payments.Data;
using FIAP.MicroService.Payments.Data.Repositories;
using FIAP.MicroService.Payments.Domain.Repositories;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

app.Run();
