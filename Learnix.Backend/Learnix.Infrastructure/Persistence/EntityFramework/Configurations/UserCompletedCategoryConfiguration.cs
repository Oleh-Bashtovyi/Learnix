using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class UserCompletedCategoryConfiguration : IEntityTypeConfiguration<UserCompletedCategory>
{
    public void Configure(EntityTypeBuilder<UserCompletedCategory> builder)
    {
        builder.ToTable("UserCompletedCategories");
        builder.HasKey(uc => new { uc.UserId, uc.CategoryId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(uc => uc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(uc => uc.UserId);
    }
}
