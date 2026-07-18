using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.DatabaseObjects;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using Learnix.Infrastructure.Persistence.EntityFramework.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>PostgreSQL: EF Core, ASP.NET Identity, interceptors and repositories.</summary>
public static class PersistenceModule
{
    /// <param name="enableDomainEvents">
    /// Off for the DbMigrator, which must not dispatch domain events while seeding.
    /// </param>
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
            // PrepareForDeletion only exists to raise domain events, so it is pointless without a
            // dispatcher — the DbMigrator runs with both off.
            services.AddSingleton<PrepareForDeletionInterceptor>();
            services.AddSingleton<DomainEventsInterceptor>();
        }

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

            // EF Core runs SaveChanges interceptors in registration order, and this order is a contract,
            // not a preference: PrepareForDeletion raises the domain events that DomainEvents then
            // collects and dispatches, so it has to come first or those events are silently dropped.
            // PrepareForDeletionInterceptorTests locks the order in.
            var interceptors = new List<IInterceptor>
            {
                sp.GetRequiredService<AuditableInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>()
            };

            if (enableDomainEvents)
            {
                interceptors.Add(sp.GetRequiredService<PrepareForDeletionInterceptor>());
                interceptors.Add(sp.GetRequiredService<DomainEventsInterceptor>());
            }

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(interceptors);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Repeatable, idempotent database objects EF cannot model (the outbox notify trigger, the
        // deferrable ordering constraints). Applied after Migrate() by both the DbMigrator and the
        // integration-test bootstrap — see DatabaseObjectsApplier.
        services.AddScoped<DatabaseObjectsApplier>();

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
}
