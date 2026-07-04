using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CourseId).IsRequired();
        builder.Property(c => c.StudentId).IsRequired();
        builder.Property(c => c.EnrollmentId).IsRequired();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(32);
        builder.Property(c => c.FileUrl).HasMaxLength(512);
        builder.Property(c => c.IssuedAt).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => new { c.StudentId, c.CourseId }).IsUnique();

        builder.HasOne<Enrollment>()
            .WithMany()
            .HasForeignKey(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
