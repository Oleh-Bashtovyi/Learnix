using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.API.Hubs;
using Learnix.API.Middleware;
using Learnix.API.Swagger;
using Learnix.Application;
using Learnix.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

// Load .env before CreateBuilder so env vars are visible to the configuration system
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (environment == Environments.Development)
{
    var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envFile))
        DotNetEnv.Env.NoClobber().Load(envFile);
}

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
builder.Services.AddPresentation();
builder.Services.AddLearnixAuthentication(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddLearnixRateLimiting();

// API versioning — URL segment (api/v1/...). AssumeDefaultVersionWhenUnspecified only matters for
// requests that bypass the versioned route template (e.g. WebApplicationFactory calls in tests
// hitting an unversioned path); real clients always go through api/v{version}/....
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

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

// Swagger — one document per registered API version (ConfigureSwaggerOptions), plus a JWT bearer
// scheme so protected endpoints can be authorized and called directly from the Swagger UI.
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Paste the access token — no \"Bearer \" prefix needed."
    });
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));
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

// Reads X-Forwarded-For / X-Forwarded-Proto headers set by the reverse proxy (Azure ACA ingress / nginx)
// so the app sees the real client IP and scheme instead of the proxy's internal address.
// Must be first — everything that follows depends on the correct IP/scheme being available.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // Serves the raw OpenAPI JSON document at /swagger/{version}/swagger.json.
    app.UseSwagger();
    // Serves the interactive Swagger UI web page for manual API testing — one dropdown entry
    // per registered API version.
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>()
            .ApiVersionDescriptions;
        foreach (var description in descriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json", $"Learnix API {description.GroupName}");
        }
        options.RoutePrefix = "swagger";
    });
}

// Enriches the Serilog log context with request-scoped properties (e.g. correlation ID, user ID)
// so every log line emitted during a request automatically includes that metadata.
app.UseMiddleware<LogEnrichmentMiddleware>();
// Emits a structured log entry for every HTTP request (method, path, status code, elapsed time).
// Placed after LogEnrichmentMiddleware so its log entry already includes the enriched properties.
app.UseSerilogRequestLogging();

// Global exception handler — catches any unhandled exception thrown later in the pipeline
// and converts it into a structured ProblemDetails JSON response (RFC 7807).
// Must be as early as possible so no exception escapes to the raw ASP.NET error page.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    // Adds the Strict-Transport-Security (HSTS) response header, instructing browsers
    // to only communicate with this site over HTTPS for a set duration.
    app.UseHsts();
    // Returns HTTP 307 Temporary Redirect to the HTTPS equivalent URL for any plain HTTP request.
    // Placed after UseHsts() so the HSTS header is already applied before the redirect fires.
    app.UseHttpsRedirection();
}

// Appends security-related HTTP response headers to every response
// (e.g. X-Content-Type-Options, X-Frame-Options, Content-Security-Policy).
// Placed after HTTPS enforcement so headers are only attached to the final HTTPS response.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Matches the incoming request URL to a registered route/endpoint definition.
// Must come before UseCors, UseAuthentication, and UseAuthorization because those
// middlewares rely on the resolved endpoint metadata to make their decisions.
app.UseRouting();

// Enforces per-client / per-endpoint request rate limits defined in AddLearnixRateLimiting().
// Placed after UseAuthentication so policies that key on user identity work correctly.
app.UseRateLimiter();

// Validates the Origin header against the configured allowed-origins list and adds
// the appropriate Access-Control-Allow-* response headers.
// Must come after UseRouting (needs endpoint) and before UseAuthentication.
app.UseCors();

// Reads the authentication cookie / JWT bearer token, validates it, and populates
// HttpContext.User with the claims principal for the current request.
app.UseAuthentication();

// Checks that the authenticated principal (HttpContext.User) has the required
// roles / policies for the matched endpoint. Returns 401/403 if not.
// Must always follow UseAuthentication.
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>(HubRoutes.Notifications);

await app.RunAsync();

// Exposed so Learnix.IntegrationTests' WebApplicationFactory<Program> can boot the real pipeline.
// A top-level-statements Program is internal by default; this is the standard way to open it.
public partial class Program
{
    protected Program()
    {
    }
}
