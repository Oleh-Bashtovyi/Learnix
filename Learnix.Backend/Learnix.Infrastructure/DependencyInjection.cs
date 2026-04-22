using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Settings;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Identity;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Persistence.Interceptors;
using Learnix.Infrastructure.Persistence.Repositories;
using Learnix.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Learnix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // App settings
        services.Configure<AppSettings>(configuration.GetSection("App"));
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<GoogleSettings>(configuration.GetSection("Google"));

        // EF Core + interceptors
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
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ASP.NET Core Identity
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

        // JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
            throw new InvalidOperationException("JWT secret is not configured.");

        services
            .AddAuthentication(options =>
            {
                // Override Identity's default cookie scheme — we use JWT everywhere.
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep raw claim names (sub, email, role)
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();

        // Fail-fast validation
        var googleSettings = configuration.GetSection("Google").Get<GoogleSettings>()
            ?? throw new InvalidOperationException("Missing 'Google' configuration section.");

        if (string.IsNullOrWhiteSpace(googleSettings.ClientId))
            throw new InvalidOperationException("Google OAuth Client ID is not configured.");

        // Auth services
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        services.AddHttpContextAccessor();

        // Course services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IPublicCourseCatalogSearchService, PublicCourseCatalogSearchService>();

        // Repositories
        services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
        services.AddScoped(typeof(IReadRepositoryBase<>), typeof(RepositoryBase<>));

        // Background services
        services.AddHostedService<RoleSeederHostedService>();
        services.AddHostedService<CategorySeederHostedService>();
        services.AddHostedService<RefreshTokenCleanupHostedService>();

        return services;
    }
}
