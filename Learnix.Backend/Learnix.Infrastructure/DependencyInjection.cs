using Anthropic.SDK;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Services;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Settings;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Hubs;
using Learnix.Infrastructure.Services.Achievements;
using Learnix.Infrastructure.AiChat.Anthropic;
using Learnix.Infrastructure.AiChat.Gemini;
using Learnix.Infrastructure.Identity;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Persistence.Interceptors;
using Learnix.Infrastructure.Persistence.Mongo;
using Learnix.Infrastructure.Persistence.Mongo.Conventions;
using Learnix.Infrastructure.Persistence.Mongo.Repositories;
using Learnix.Infrastructure.Persistence.Repositories;
using Learnix.Infrastructure.Services;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Reflection;
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
        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));

        // EF Core + interceptors
        services.AddSingleton<AuditableInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<DomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditableInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<DomainEventsInterceptor>());
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

                // SignalR WebSocket connections can't set headers — accept token via query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        // Fail-fast validation
        var googleSettings = configuration.GetSection("Google").Get<GoogleSettings>()
            ?? throw new InvalidOperationException("Missing 'Google' configuration section.");

        if (string.IsNullOrWhiteSpace(googleSettings.ClientId))
            throw new InvalidOperationException("Google OAuth Client ID is not configured.");

        // Auth services
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<OutboxDbContextHolder>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        services.AddHttpContextAccessor();

        // Course services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPublicCourseCatalogSearchService, PublicCourseCatalogSearchService>();
        services.AddScoped<IFeaturedCoursesService, FeaturedCoursesService>();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstructorApplicationRepository, InstructorApplicationRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        services.AddScoped<ICourseReviewRepository, CourseReviewRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        services.AddScoped<IUserAchievementProgressRepository, UserAchievementProgressRepository>();
        services.AddScoped<IUserCompletedCategoryRepository, UserCompletedCategoryRepository>();

        // Achievements
        services.AddScoped<IAchievementEvaluator, AchievementEvaluator>();
        services.AddSignalR();
        services.AddScoped<IAchievementNotifier, SignalRAchievementNotifier>();

        // Register MediatR notification handlers defined in the Infrastructure assembly
        // (e.g., outbox event handlers that translate domain events into outbox messages).
        // The Application-layer AddMediatR only scans its own assembly.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Storage
        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("AzureBlobStorage")
                ?? throw new InvalidOperationException("AzureBlobStorage connection string is missing");
            return new BlobServiceClient(connectionString);
        });
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // MongoDB
        services.Configure<MongoSettings>(configuration.GetSection("Mongo"));
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            MongoConventionRegistration.Register();
            return new MongoClient(settings.ConnectionString);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        // AI Chat — provider (swap by changing AiChat:Provider in config)
        services.Configure<AnthropicSettings>(configuration.GetSection("Anthropic"));
        services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
        services.Configure<AiChatSettings>(configuration.GetSection("AiChat"));

        var aiProvider = configuration["AiChat:Provider"] ?? "Anthropic";
        if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiChatProvider, GeminiChatProvider>(client =>
                client.BaseAddress = new Uri(
                    configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com"));
        }
        else
        {
            services.AddSingleton(sp =>
                new AnthropicClient(new APIAuthentication(
                    sp.GetRequiredService<IOptions<AnthropicSettings>>().Value.ApiKey)));
            services.AddScoped<IAiChatProvider, AnthropicChatProvider>();
        }

        // AI Chat — tools and orchestrator
        services.AddScoped<IChatTool, SearchCoursesTool>();
        services.AddScoped<ChatStreamOrchestrator>();

        // Background services
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        services.AddHostedService<RoleSeederHostedService>();
        services.AddHostedService<CategorySeederHostedService>();
        services.AddHostedService<BlobStorageBootstrapper>();
        services.AddHostedService<RefreshTokenCleanupHostedService>();
        services.AddHostedService<OutboxProcessorService>();
        services.AddHostedService<CertificatePdfGenerationService>();
        services.AddHostedService<MongoIndexInitializer>();
        services.AddHostedService<ChatSessionCleanupService>();
        services.AddHostedService<CategoryCoursesCountReconciliationService>();

        return services;
    }
}
