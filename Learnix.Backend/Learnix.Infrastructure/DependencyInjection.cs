using Learnix.Application.Common.Interfaces;
using Learnix.Application.Common.Settings;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Identity;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Persistence.Interceptors;
using Learnix.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── App settings ───────────────────────────────────────────────
        services.Configure<AppSettings>(configuration.GetSection("App"));

        // ── EF Core + interceptors (existing setup, untouched) ─────────
        services.AddSingleton<AuditableInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditableInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ── ASP.NET Core Identity ──────────────────────────────────────
        services
            .AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequiredLength = AuthValidationConstants.PasswordMinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ── Auth services ──────────────────────────────────────────────
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

        services.AddHostedService<RoleSeederHostedService>();

        return services;
    }
}
