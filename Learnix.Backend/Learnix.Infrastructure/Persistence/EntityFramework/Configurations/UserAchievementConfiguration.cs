using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");
        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(ua => ua.UnlockedAt).IsRequired();
        builder.Property(ua => ua.Seen).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ua => new { ua.UserId, ua.Code }).IsUnique();
    }
}
