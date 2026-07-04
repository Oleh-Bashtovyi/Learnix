using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("LessonProgresses");
        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.IsCompleted).IsRequired();
        builder.Property(lp => lp.LastAccessedAt).IsRequired();
        builder.Property(lp => lp.CompletedAt);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(lp => lp.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(lp => lp.StudentId);
        builder.HasIndex(lp => lp.CourseId);
        builder.HasIndex(lp => new { lp.StudentId, lp.LessonId }).IsUnique();
    }
}
