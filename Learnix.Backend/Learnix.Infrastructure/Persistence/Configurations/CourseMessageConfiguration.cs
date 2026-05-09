using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.Configurations;

public sealed class CourseMessageConfiguration : IEntityTypeConfiguration<CourseMessage>
{
    public void Configure(EntityTypeBuilder<CourseMessage> builder)
    {
        builder.ToTable("CourseMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(ConversationConstants.MessageMaxLength);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CourseConversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
    }
}
