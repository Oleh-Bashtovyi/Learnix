using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Domain.Common;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Learnix.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

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
