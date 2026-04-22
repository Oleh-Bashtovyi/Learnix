using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.Configurations;

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(SectionConstants.TitleMaxLength);

        builder.Property(s => s.DisplayOrder).IsRequired();

        builder.HasMany(s => s.Lessons)
            .WithOne()
            .HasForeignKey(l => l.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Section.Lessons))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Compact ordering (ADR-040 cont.): unique (CourseId, Order).
        builder.HasIndex(s => new { s.CourseId, s.DisplayOrder }).IsUnique();
    }
}
