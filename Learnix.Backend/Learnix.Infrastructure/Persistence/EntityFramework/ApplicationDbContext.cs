using System.Linq.Expressions;
using System.Reflection;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Domain.Common;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await Database.BeginTransactionAsync(ct);
            await work();
            await tx.CommitAsync(ct);
        });
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<InstructorApplication> InstructorApplications => Set<InstructorApplication>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
    public DbSet<CourseConversation> CourseConversations => Set<CourseConversation>();
    public DbSet<CourseMessage> CourseMessages => Set<CourseMessage>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<UserAchievementProgress> UserAchievementProgresses => Set<UserAchievementProgress>();
    public DbSet<UserCompletedCategory> UserCompletedCategories => Set<UserCompletedCategory>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // BaseEntity always generates Guid PKs on the client side (Guid.NewGuid()),
        // so we must tell EF Core not to expect database-generated keys.
        // Without this, EF's DetectChanges misidentifies new child entities
        // (added via backing fields / navigation properties) as existing entities,
        // generating UPDATE instead of INSERT → DbUpdateConcurrencyException.
        foreach (var entityType in builder.Model
            .GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            var pk = entityType.FindPrimaryKey();
            if (pk?.Properties.Count == 1 && pk.Properties[0].ClrType == typeof(Guid))
            {
                pk.Properties[0].ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }

        foreach (var entityType in builder.Model
            .GetEntityTypes()
            .Where(e => typeof(IHasDomainEvents)
            .IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }

        foreach (var type in builder.Model
            .GetEntityTypes()
            .Select(e => e.ClrType)
            .Where(type => typeof(ISoftDeletable)
            .IsAssignableFrom(type)))
        {
            var parameter = Expression.Parameter(type, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);

            builder.Entity(type).HasQueryFilter(filter);
        }
    }
}
