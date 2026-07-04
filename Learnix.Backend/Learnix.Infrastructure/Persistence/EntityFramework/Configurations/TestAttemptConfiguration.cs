using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("TestAttempts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CourseId).IsRequired();
        builder.Property(a => a.TestLessonId).IsRequired();
        builder.Property(a => a.StudentId).IsRequired();
        builder.Property(a => a.AttemptNumber).IsRequired();
        builder.Property(a => a.StartedAt).IsRequired();

        builder.OwnsMany(a => a.Answers, ab => ab.ToJson());

        builder.Navigation(a => a.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(a => a.TestLessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Partial unique index: at most one in-progress attempt per student per test.
        // Enforces DB-level idempotency for concurrent start calls.
        // Also covers general (StudentId, TestLessonId) lookups via index scan for in-progress queries.
        // Submitted-attempt queries use a seq scan on small datasets; add a separate index if scale demands it.
        builder.HasIndex(a => new { a.StudentId, a.TestLessonId })
            .HasFilter("\"SubmittedAt\" IS NULL")
            .IsUnique()
            .HasDatabaseName("IX_TestAttempts_OneInProgress");
    }
}
