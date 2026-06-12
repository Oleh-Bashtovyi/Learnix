using Learnix.API.Extensions;
using Learnix.API.Middleware;
using Learnix.Application;
using Learnix.Infrastructure;
using Learnix.Infrastructure.Hubs;
using Learnix.Infrastructure.Persistence;
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
// Trusted proxy IPs are read from "Proxy:TrustedProxies" in configuration.
// - Production  : list your reverse-proxy IP(s) there so only those headers are accepted.
// - Development : the section is left empty and we fall back to trust-all (Docker, Vite, etc.).
// Never use trust-all in staging/production — it lets any client spoof X-Forwarded-For
// and bypass IP-based rate limiting.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var trustedProxies = builder.Configuration
        .GetSection("Proxy:TrustedProxies")
        .Get<string[]>() ?? [];

    if (trustedProxies.Length > 0)
    {
        // Production path: only accept forwarded headers from the listed proxy IPs.
        foreach (var ip in trustedProxies)
        {
            if (System.Net.IPAddress.TryParse(ip, out var parsed))
                options.KnownProxies.Add(parsed);
        }
    }
    else if (builder.Environment.IsDevelopment())
    {
        // Development path: trust every hop so local tooling (Docker bridge, Vite proxy)
        // can forward headers without explicit registration.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
    // If neither applies (staging/prod with no proxies configured) the ASP.NET Core
    // defaults remain active: only loopback (127.0.0.1 / ::1) is trusted.
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
