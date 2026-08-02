using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class TestVersionConfiguration : IEntityTypeConfiguration<TestVersion>
{
    public void Configure(EntityTypeBuilder<TestVersion> builder)
    {
        builder.ToTable("TestVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.TestLessonId).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(v => v.TestLessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Version numbers are assigned by TestVersion.Branch from the version it branched off, so two
        // concurrent edits of the same test would both propose N+1. The unique index is what turns that
        // into a failed save rather than two editions claiming to be the same one.
        builder.HasIndex(v => new { v.TestLessonId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_TestVersions_LessonVersion");

        // The same JSON document that used to live on Lessons.Questions, moved here wholesale — see
        // LessonConfiguration for why the collections have to be reached through their backing fields.
        builder.OwnsMany(v => v.Questions, qb =>
        {
            qb.ToJson();

            qb.Ignore(q => q.Id);

            qb.OwnsOne(q => q.TextAnswer);
            qb.OwnsMany(q => q.Options, ob =>
            {
                ob.Ignore(o => o.Id);
            });

            qb.Navigation(q => q.Options)
                .HasField("_options")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(v => v.Questions)
            .HasField("_questions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
