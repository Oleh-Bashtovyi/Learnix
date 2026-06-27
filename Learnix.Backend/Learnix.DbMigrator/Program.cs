using Learnix.Application;
using Learnix.DbMigrator.Seeders;
using Learnix.Infrastructure;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Load .env from Learnix.API if it exists (for local development)
var apiEnvFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "Learnix.API", ".env");
if (File.Exists(apiEnvFile))
    DotNetEnv.Env.NoClobber().Load(apiEnvFile);

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((context, config) =>
{
    var apiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Learnix.API");
    config.AddJsonFile(Path.Combine(apiPath, "appsettings.json"), optional: true);
    config.AddJsonFile(Path.Combine(apiPath, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true);
});

builder.ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;

    // Core registrations (needed for DbContext, Identity, etc.)
    services.AddApplication();
    services.AddInfrastructure(configuration);

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

    logger.LogInformation("Initializing local Blob Storage...");
    var storageSeeder = services.GetRequiredService<StorageSeeder>();
    await storageSeeder.SeedAsync();

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
