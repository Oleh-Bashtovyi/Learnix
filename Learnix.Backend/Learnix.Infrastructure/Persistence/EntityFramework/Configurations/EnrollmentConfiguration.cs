using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.PaymentStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.PricePaid)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(e => e.EnrolledAt).IsRequired();
        builder.Property(e => e.CompletedAt);

        builder.HasOne(e => e.Course)
            .WithMany()
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
    }
}
