using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(CourseConstants.TitleMaxLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(CourseConstants.DescriptionMaxLength);

        builder.Property(c => c.CoverBlobPath)
            .HasMaxLength(CourseConstants.CoverImageUrlMaxLength);

        builder.Property(c => c.Price)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.EnrollmentsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.ReviewsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.AverageRating)
            .IsRequired()
            .HasPrecision(4, 2)
            .HasDefaultValue(0m);

        // Tags as Postgres text[] (EF Core 8 + Npgsql support this natively).
        //
        // Npgsql maps this as a native array, not as an EF primitive collection, and that mapping
        // has no SelectMany translation on EF 8: LINQ can test the array (Contains, Length) but
        // cannot group by its elements. A query that needs the tags unnested writes that SQL
        // itself — see CourseRepository.GetPopularTagsAsync.
        builder.Property(c => c.Tags)
            .HasColumnName("Tags")
            .HasColumnType("text[]");

        // Soft delete
        builder.Property(c => c.IsDeleted).IsRequired();
        builder.Property(c => c.DeletedAt);

        // FK > Category (Restrict: block deletion of category that has courses, see ADR-016).
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK > User (instructor). No navigation, we only keep InstructorId for simplicity.
        // Restrict on delete: User is soft-deletable, so FK integrity stays sound.

        // Sections via backing field.
        builder.HasMany(c => c.Sections)
            .WithOne()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Course.Sections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.InstructorId);
        builder.HasIndex(c => c.CategoryId);
        builder.HasIndex(c => c.Status);

        // Full-text search (ADR-BACK-CATALOG-001): a generated, weighted tsvector — Title outranks
        // Description outranks Tags — queried through Learnix.Infrastructure.Services.Search.
        // CourseFullTextSearchExtensions. A shadow property: Course (Domain) gets no new member, and
        // Application never sees NpgsqlTsVector, keeping the Application layer free of EF Core.
        //
        // Tags go through array_to_tsvector (one lexeme per array element, no parsing) rather than
        // to_tsvector on a joined string, because array_to_string/anyarray::text are both STABLE in
        // Postgres — not permitted in a generated column — while array_to_tsvector is IMMUTABLE.
        // Trade-off: unlike Title/Description, tag lexemes are not lowercased, so a tag stored with
        // uppercase letters only matches a search typed in the same case. Tags carry the lowest
        // weight (C); Title and Description, the dominant signal, are fully normalized.
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("SearchVector")
            .HasComputedColumnSql(
                """
                setweight(to_tsvector('english'::regconfig, coalesce("Title", '')), 'A') ||
                setweight(to_tsvector('english'::regconfig, coalesce("Description", '')), 'B') ||
                setweight(array_to_tsvector(coalesce("Tags", ARRAY[]::text[])), 'C')
                """,
                stored: true);

        builder.HasIndex("SearchVector").HasMethod("gin");
    }
}
