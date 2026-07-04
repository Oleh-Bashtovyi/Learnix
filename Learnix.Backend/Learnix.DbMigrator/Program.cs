using Learnix.DbMigrator.Seeders;
using Learnix.Infrastructure;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Resolve Learnix.API path robustly
var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
while (currentDir != null && !Directory.Exists(Path.Combine(currentDir.FullName, "Learnix.API")))
{
    currentDir = currentDir.Parent;
}

if (currentDir == null)
{
    throw new DirectoryNotFoundException("Could not find the 'Learnix.API' directory.");
}

var apiPath = Path.Combine(currentDir.FullName, "Learnix.API");

// Load .env from Learnix.API if it exists (for local development)
var apiEnvFile = Path.Combine(apiPath, ".env");
if (File.Exists(apiEnvFile))
    DotNetEnv.Env.NoClobber().Load(apiEnvFile);

var builder = Host.CreateDefaultBuilder(args);

// Enable reading ASPNETCORE_ENVIRONMENT to match web project behavior
builder.ConfigureHostConfiguration(config =>
{
    config.AddEnvironmentVariables(prefix: "ASPNETCORE_");
});

builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile(Path.Combine(apiPath, "appsettings.json"), optional: true);
    config.AddJsonFile(Path.Combine(apiPath, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true);
    config.AddEnvironmentVariables();
    config.AddCommandLine(args);
});

builder.ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;

    // Core registrations (needed for DbContext, Identity, etc.)

    // Infrastructure (Targeted for Migrator)
    services.AddPersistence(configuration, enableDomainEvents: false);
    services.AddStorage(configuration);

    // Register Seeders
    services.AddScoped<AdminSeeder>();
    services.AddScoped<RoleSeeder>();
    services.AddScoped<CategorySeeder>();
    services.AddScoped<CourseSeeder>();
    services.AddScoped<StudentSeeder>();
    services.AddScoped<StorageSeeder>();
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Applying Entity Framework migrations...");
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Migrations applied successfully.");

    if (args.Contains("--create-blob"))
    {
        logger.LogInformation("Initializing local Blob Storage (--create-blob flag detected)...");
        var storageSeeder = services.GetRequiredService<StorageSeeder>();
        await storageSeeder.SeedAsync();
    }
    else
    {
        logger.LogInformation("Skipping Blob Storage initialization. Use --create-blob flag to initialize containers locally.");
    }

    logger.LogInformation("Running System Seeders...");
    var roleSeeder = services.GetRequiredService<RoleSeeder>();
    await roleSeeder.SeedAsync();

    var adminSeeder = services.GetRequiredService<AdminSeeder>();
    await adminSeeder.SeedAsync();

    var categorySeeder = services.GetRequiredService<CategorySeeder>();
    await categorySeeder.SeedAsync();

    if (args.Contains("--seed-demo"))
    {
        logger.LogInformation("Running Demo Seeders (--seed-demo flag detected)...");
        var courseSeeder = services.GetRequiredService<CourseSeeder>();
        await courseSeeder.SeedAsync();

        var studentSeeder = services.GetRequiredService<StudentSeeder>();
        await studentSeeder.SeedAsync();
    }
    else
    {
        logger.LogInformation("Skipping Demo Seeders. Use --seed-demo flag to generate fake content.");
    }

    logger.LogInformation("Database initialization completed successfully.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "An error occurred during database initialization.");
    Environment.Exit(1);
}

Environment.Exit(0);
