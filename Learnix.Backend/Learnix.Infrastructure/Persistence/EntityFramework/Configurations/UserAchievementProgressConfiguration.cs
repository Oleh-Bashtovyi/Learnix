using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class UserAchievementProgressConfiguration : IEntityTypeConfiguration<UserAchievementProgress>
{
    public void Configure(EntityTypeBuilder<UserAchievementProgress> builder)
    {
        builder.ToTable("UserAchievementProgresses");
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId).ValueGeneratedNever();
        builder.Property(p => p.LessonsCompleted).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.CoursesCompleted).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.DistinctCategoriesCompleted).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.ProfileCompleted).IsRequired().HasDefaultValue(false);

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserAchievementProgress>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
