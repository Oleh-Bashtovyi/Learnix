using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

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

        // Compact ordering (ADR-040 cont.): unique (CourseId, DisplayOrder). The uniqueness is *not*
        // modelled here — it is a DEFERRABLE unique constraint applied by the repeatable script
        // DatabaseObjects/ordering_deferrable.sql, so a reorder permutation is validated at COMMIT rather
        // than per-row. Modelling it as an EF unique index would re-introduce the per-row check; modelling
        // it as an alternate key would freeze DisplayOrder (EF forbids mutating key columns). So EF leaves
        // (CourseId, DisplayOrder) unconstrained and the script owns it — EF still keeps its own plain
        // index on the CourseId foreign key.
    }
}
