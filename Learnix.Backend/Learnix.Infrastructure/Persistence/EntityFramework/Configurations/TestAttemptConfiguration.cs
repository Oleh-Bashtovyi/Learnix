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
        builder.Property(a => a.TestVersionId).IsRequired();
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

        // The version an attempt was taken against is the only thing that makes its answers legible, so
        // nothing may delete it out from under the attempt.
        //
        // NoAction rather than Restrict, and the difference matters here: both refuse the delete, but
        // Restrict checks immediately while NoAction checks at the end of the statement. Deleting a
        // lesson cascades into TestVersions and TestAttempts at once, and under Restrict that races —
        // the version's delete can be rejected before the cascade has cleared the attempts pointing at
        // it. NoAction lets the whole statement settle first and then finds nothing left to complain
        // about, which is the only reading under which deleting a lesson works at all.
        builder.HasOne<TestVersion>()
            .WithMany()
            .HasForeignKey(a => a.TestVersionId)
            .OnDelete(DeleteBehavior.NoAction);

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
