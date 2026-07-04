using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class InstructorApplicationConfiguration : IEntityTypeConfiguration<InstructorApplication>
{
    public void Configure(EntityTypeBuilder<InstructorApplication> builder)
    {
        builder.ToTable("InstructorApplications");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.UserId).IsRequired();

        builder.Property(a => a.MotivationText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.PortfolioUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(a => a.ReviewedByAdminId);
        builder.Property(a => a.ReviewedAt);

        // Unique index enforces one application per user at the DB level
        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasDatabaseName("IX_InstructorApplications_UserId");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReviewedByAdminId — restrict delete to avoid cascade cycle (User → Application → User)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
