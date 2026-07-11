using System.Reflection;
using System.Text;
using Anthropic.SDK;
using Azure.Storage.Blobs;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Services;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Settings;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Sections.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.AiChat;
using Learnix.Infrastructure.AiChat.Anthropic;
using Learnix.Infrastructure.AiChat.Gemini;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Email;
using Learnix.Infrastructure.Identity;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.Handlers;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using Learnix.Infrastructure.Persistence.EntityFramework.Repositories;
using Learnix.Infrastructure.Persistence.Mongo;
using Learnix.Infrastructure.Persistence.Mongo.Conventions;
using Learnix.Infrastructure.Persistence.Mongo.Repositories;
using Learnix.Infrastructure.Services.Achievements;
using Learnix.Infrastructure.Services.Catalog;
using Learnix.Infrastructure.Services.Certificates;
using Learnix.Infrastructure.Services.HostedServices.Cleanup;
using Learnix.Infrastructure.Services.HostedServices.Maintenance;
using Learnix.Infrastructure.Services.Outbox;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace Learnix.Infrastructure;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-005: JWT secret — placeholder in base + dev-secret in Development + env var in production
/// - ADR-BACK-AUTH-015: Infrastructure gets FrameworkReference to Microsoft.AspNetCore.App
/// - ADR-BACK-AUTH-016: 6-Digit OTP for Email Confirmation instead of Magic Link
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddStorage(configuration);
        services.AddAuth(configuration);
        services.AddExternalServices(configuration);

        return services;
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableDomainEvents = true)
    {
        // EF Core + interceptors
        services.AddSingleton<AuditableInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        if (enableDomainEvents)
        {
            services.AddSingleton<DomainEventsInterceptor>();
        }

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

            var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>
            {
                sp.GetRequiredService<AuditableInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>()
            };

            if (enableDomainEvents)
            {
                interceptors.Add(sp.GetRequiredService<DomainEventsInterceptor>());
            }

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(interceptors);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ASP.NET Core Identity
        services
            .AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                options.Password.RequiredLength = AuthValidationConstants.PasswordMinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstructorApplicationRepository, InstructorApplicationRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        services.AddScoped<ICourseReviewRepository, CourseReviewRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        services.AddScoped<IUserAchievementProgressRepository, UserAchievementProgressRepository>();
        services.AddScoped<IUserCompletedCategoryRepository, UserCompletedCategoryRepository>();

        // Messaging
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Notifications
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BlobStorageOptions>(configuration.GetSection(ConfigurationSectionNameCaonstants.BlobStorage));

        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("AzureBlobStorage");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("AzureBlobStorage connection string is missing or empty.");
            }

            return new BlobServiceClient(connectionString);
        });
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }

    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Jwt));
        services.Configure<GoogleSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Google));

        // JWT Authentication
        var jwtSettings = configuration.GetSection(ConfigurationSectionNameCaonstants.Jwt).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
            throw new InvalidOperationException("JWT secret is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy("EmailConfirmed", policy =>
                policy.RequireClaim("email_verified", "true"));
        });

        // Fail-fast validation
        var googleSettings = configuration.GetSection(ConfigurationSectionNameCaonstants.Google).Get<GoogleSettings>()
            ?? throw new InvalidOperationException("Missing 'Google' configuration section.");

        if (string.IsNullOrWhiteSpace(googleSettings.ClientId))
            throw new InvalidOperationException("Google OAuth Client ID is not configured.");

        // Auth services
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IChangePasswordService, ChangePasswordService>();
        services.AddScoped<ISetPasswordService, SetPasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRoleService, UserRoleService>();

        return services;
    }

    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("App"));

        // Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
        });

        services.AddScoped<OutboxDbContextHolder>();
        services.AddOutboxMessageHandlers();
        services.Configure<SmtpSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Smtp));
        services.AddLocalization();
        services.AddSingleton<EmailRenderer>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Course services
        services.AddScoped<IPublicCourseCatalogSearchService, PublicCourseCatalogSearchService>();
        services.AddScoped<IFeaturedCoursesService, FeaturedCoursesService>();

        // Achievements
        services.AddScoped<IAchievementEvaluator, AchievementEvaluator>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // MongoDB
        services.Configure<MongoSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Mongo));
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            MongoConventionRegistration.Register();
            return new MongoClient(settings.ConnectionString);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        // AI Chat
        services.Configure<AnthropicSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Anthropic));
        services.Configure<GeminiSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.Gemini));
        services.Configure<AiChatSettings>(configuration.GetSection(ConfigurationSectionNameCaonstants.AiChat));

        var aiProvider = configuration[$"{ConfigurationSectionNameCaonstants.AiChat}:Provider"] ?? "Anthropic";
        if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAiChatProvider, GeminiChatProvider>();
        }
        else
        {
            services.AddSingleton(sp =>
                new AnthropicClient(new APIAuthentication(
                    sp.GetRequiredService<IOptions<AnthropicSettings>>().Value.ApiKey)));
            services.AddScoped<IAiChatProvider, AnthropicChatProvider>();
        }

        services.AddScoped<IChatTool, SearchCoursesTool>();
        services.AddScoped<IChatTool, GetCategoriesTool>();
        services.AddScoped<IChatTool, GetInstructorCoursesTool>();
        services.AddScoped<IChatTool, GetMyLearningProfileTool>();
        services.AddScoped<IChatTool, GetCurrentLessonTool>();
        services.AddScoped<IChatTool, GetTestReviewTool>();
        services.AddSingleton<IChatTool, GetPlatformInfoTool>();
        services.AddScoped<ChatScopeAuthorizer>();
        services.AddScoped<ChatStreamOrchestrator>();
        services.AddScoped<IAiAvailabilityStore, RedisAiAvailabilityStore>();

        // Background services
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddSingleton<ICertificatePdfGenerator, CertificatePdfGenerator>();

        services.AddHostedService<RefreshTokenCleanupHostedService>();
        services.AddHostedService<DeletedAccountPurgeService>();
        services.AddSingleton<OutboxSignal>();
        services.AddHostedService<OutboxNotificationListener>();
        services.AddHostedService<OutboxProcessorService>();
        services.AddHostedService<MongoIndexInitializer>();
        services.AddHostedService<CategoryCoursesCountReconciliationService>();
        services.AddHostedService<CourseRatingReconciliationService>();

        return services;
    }
}
