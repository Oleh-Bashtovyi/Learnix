using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(LessonConstants.TitleMaxLength);

        builder.Property(l => l.DisplayOrder).IsRequired();

        // TPH: single Lessons table with LessonType discriminator (ADR-008).
        builder.HasDiscriminator(l => l.LessonType)
            .HasValue<VideoLesson>(LessonType.Video)
            .HasValue<PostLesson>(LessonType.Post)
            .HasValue<TestLesson>(LessonType.Test);

        builder.HasIndex(l => new { l.SectionId, l.DisplayOrder }).IsUnique();
    }
}

public sealed class VideoLessonConfiguration : IEntityTypeConfiguration<VideoLesson>
{
    public void Configure(EntityTypeBuilder<VideoLesson> builder)
    {
        // NOTE: TPH tradeoff — VideoUrl is required by domain but nullable at DB level
        // because PostLesson / TestLesson rows won't have it.
        // Domain constructors + UpdateVideo enforce invariant; no DB CHECK constraint by choice.
        builder.Property(v => v.VideoBlobPath)
            .HasMaxLength(LessonConstants.VideoUrlMaxLength);

        builder.Property(v => v.Description)
            .HasMaxLength(LessonConstants.DescriptionMaxLength);
    }
}

public sealed class PostLessonConfiguration : IEntityTypeConfiguration<PostLesson>
{
    public void Configure(EntityTypeBuilder<PostLesson> builder)
    {
        builder.Property(p => p.Content)
            .HasMaxLength(LessonConstants.ContentMaxLength);
    }
}

public sealed class TestLessonConfiguration : IEntityTypeConfiguration<TestLesson>
{
    public void Configure(EntityTypeBuilder<TestLesson> builder)
    {
        builder.Property(t => t.Description)
            .HasMaxLength(LessonConstants.DescriptionMaxLength);

        builder.Property(t => t.PassingThreshold).IsRequired();

        // Stored as its int value, so the ladder in TestReviewMode survives into the database.
        //
        // Deliberately no HasDefaultValue: ScoreOnly is 0, which is also the CLR default, so EF would
        // treat a genuine "ScoreOnly" as "unset" and substitute the database default — silently turning
        // the strictest mode into the most permissive one. The property initialiser on the entity gives
        // new tests their FullReview default, and the migration backfills the existing rows.
        builder.Property(t => t.ReviewMode)
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsMany(t => t.Questions, qb =>
        {
            qb.ToJson();

            qb.Ignore(q => q.Id);

            qb.OwnsOne(q => q.TextAnswer);
            qb.OwnsMany(q => q.Options, ob =>
            {
                ob.Ignore(o => o.Id);
            });

            // Same reason as _questions below: EF adds to the collection while materializing, so it has
            // to write to the mutable backing field, not to the IReadOnlyList the property exposes.
            qb.Navigation(q => q.Options)
                .HasField("_options")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Instruct EF Core to write directly to the private _questions field
        builder.Navigation(t => t.Questions)
            .HasField("_questions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
