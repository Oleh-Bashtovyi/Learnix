using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(CategoryConstants.NameMaxLength);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(CategoryConstants.SlugMaxLength);

        builder.Property(c => c.IsSystem).IsRequired();

        builder.Property(c => c.ImageBlobPath)
            .HasMaxLength(500);

        builder.Property(c => c.CoursesCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}
