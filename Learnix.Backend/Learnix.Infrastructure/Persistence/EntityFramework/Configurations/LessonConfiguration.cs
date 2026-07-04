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

        builder.OwnsMany(t => t.Questions, qb =>
        {
            qb.ToJson();

            qb.Ignore(q => q.Id);

            qb.OwnsOne(q => q.TextAnswer);
            qb.OwnsMany(q => q.Options, ob =>
            {
                ob.Ignore(o => o.Id);
            });
        });

        // Instruct EF Core to write directly to the private _questions field
        builder.Navigation(t => t.Questions)
            .HasField("_questions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
