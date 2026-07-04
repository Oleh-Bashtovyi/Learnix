using Learnix.API.Extensions;
using Learnix.API.Middleware;
using Learnix.Application;
using Learnix.Infrastructure;
using Learnix.Infrastructure.Hubs;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

// Load .env before CreateBuilder so env vars are visible to the configuration system
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
    DotNetEnv.Env.NoClobber().Load(envFile);

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddLearnixRateLimiting();

// Forwarder
// Azure Container Apps sits behind Azure's internal load balancer whose IP is not static
// and cannot be pinned in configuration. ACA already enforces network-level isolation
// (the container is not publicly reachable except through the managed ingress), so trusting
// all forwarded headers is safe and is the approach recommended by Microsoft for ACA.
//
// If Proxy:TrustedProxies is set (e.g. when self-hosting behind a known reverse proxy),
// only those IPs are trusted. Otherwise we clear the allow-list and accept all hops —
// which is correct for ACA but would be unsafe behind an arbitrary public proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var trustedProxies = builder.Configuration
        .GetSection("Proxy:TrustedProxies")
        .Get<string[]>() ?? [];

    if (trustedProxies.Length > 0)
    {
        // Explicit proxy IPs provided — only trust those (e.g. self-hosted nginx/Caddy).
        foreach (var ip in trustedProxies)
        {
            if (System.Net.IPAddress.TryParse(ip, out var parsed))
                options.KnownProxies.Add(parsed);
        }
    }
    else
    {
        // No explicit IPs configured: trust all hops.
        // Safe for Azure Container Apps (network-isolated by ACA ingress).
        // Also covers local development (Docker bridge, Vite proxy, etc.).
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Learnix API",
        Version = "v1",
        Description = "Learning Management System — REST API"
    });
});

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Pipeline
var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Learnix API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

await app.RunAsync();
