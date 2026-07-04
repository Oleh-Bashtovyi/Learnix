using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(CourseConstants.TitleMaxLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(CourseConstants.DescriptionMaxLength);

        builder.Property(c => c.CoverBlobPath)
            .HasMaxLength(CourseConstants.CoverImageUrlMaxLength);

        builder.Property(c => c.Price)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.EnrollmentsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.ReviewsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.AverageRating)
            .IsRequired()
            .HasPrecision(4, 2)
            .HasDefaultValue(0m);

        // Tags as Postgres text[] (EF Core 8 + Npgsql support this natively).
        builder.Property(c => c.Tags)
            .HasColumnName("Tags")
            .HasColumnType("text[]");

        // Soft delete
        builder.Property(c => c.IsDeleted).IsRequired();
        builder.Property(c => c.DeletedAt);

        // FK > Category (Restrict: block deletion of category that has courses, see ADR-016).
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK > User (instructor). No navigation, we only keep InstructorId for simplicity.
        // Restrict on delete: User is soft-deletable, so FK integrity stays sound.

        // Sections via backing field.
        builder.HasMany(c => c.Sections)
            .WithOne()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Course.Sections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.InstructorId);
        builder.HasIndex(c => c.CategoryId);
        builder.HasIndex(c => c.Status);
    }
}
