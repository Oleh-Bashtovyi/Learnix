using Learnix.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(m => m.OccurredAt)
            .IsRequired();

        builder.Property(m => m.ProcessedAt);

        builder.Property(m => m.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.LastAttemptAt);

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        builder.Property(m => m.NextRetryAt);

        // Composite index — picks up unprocessed messages ready for retry, ordered for FIFO.
        // Matches the WHERE clause of the outbox processor's polling query.
        builder.HasIndex(m => new { m.ProcessedAt, m.NextRetryAt, m.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Processing");
    }
}
