using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.PaymentProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Enrollment)
            .WithOne()
            .HasForeignKey<Payment>(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CourseId);
        builder.HasIndex(p => p.EnrollmentId).IsUnique();
    }
}
