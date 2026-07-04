using System.Text.Json;

namespace Learnix.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }

    // for exponential backoff
    public DateTime? NextRetryAt { get; set; }

    public static OutboxMessage Create<TPayload>(Guid id, string type, TPayload payload) => new()
    {
        Id = id,
        Type = type,
        Payload = JsonSerializer.Serialize(payload),
        OccurredAt = DateTime.UtcNow,
        NextRetryAt = DateTime.UtcNow,
    };
}
