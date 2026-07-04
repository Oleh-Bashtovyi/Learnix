using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(NotificationConstants.TitleMaxLength);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(NotificationConstants.BodyMaxLength);
        builder.Property(n => n.IsRead).IsRequired().HasDefaultValue(false);

        builder.HasIndex(n => new { n.UserId, n.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
