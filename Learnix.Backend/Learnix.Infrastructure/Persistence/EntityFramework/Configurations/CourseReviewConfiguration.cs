using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
{
    public void Configure(EntityTypeBuilder<CourseReview> builder)
    {
        builder.ToTable("CourseReviews");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating).IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(2000);

        builder.HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CourseId);
        builder.HasIndex(r => new { r.StudentId, r.CourseId }).IsUnique();
    }
}
