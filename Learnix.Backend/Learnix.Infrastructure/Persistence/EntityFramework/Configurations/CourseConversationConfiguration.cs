using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class CourseConversationConfiguration : IEntityTypeConfiguration<CourseConversation>
{
    public void Configure(EntityTypeBuilder<CourseConversation> builder)
    {
        builder.ToTable("CourseConversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.StudentUnreadCount).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.InstructorUnreadCount).IsRequired().HasDefaultValue(0);

        builder.Property(c => c.LastMessagePreview)
            .HasMaxLength(ConversationConstants.LastMessagePreviewMaxLength);

        builder.HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Instructor)
            .WithMany()
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.StudentId);
        builder.HasIndex(c => c.InstructorId);
        builder.HasIndex(c => new { c.CourseId, c.StudentId }).IsUnique();
    }
}
