using Learnix.API.Extensions;
using Learnix.API.Middleware;
using Learnix.Application;
using Learnix.Infrastructure;
using Learnix.Infrastructure.Hubs;
using Learnix.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // We're clearing the default networks so we can trust headers from any proxy.
    // In a production environment, should add the specific IP addresses of reverse proxy here for security.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
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

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
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
app.MapHub<AchievementsHub>("/hubs/achievements");
app.MapHub<ChatHub>("/hubs/chat");

await app.RunAsync();
