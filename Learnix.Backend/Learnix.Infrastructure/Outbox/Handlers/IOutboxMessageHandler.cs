using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.Handlers;

/// <summary>
/// Performs the side-effect of one outbox message type. One handler, one type, and only the dependencies that
/// type actually needs — the processor stays a piece of plumbing that knows how to lock rows and retry them,
/// not a catalogue of every email and achievement in the system (ADR-INFRA-013).
/// </summary>
public interface IOutboxMessageHandler
{
    /// <summary>The value in <c>OutboxMessage.Type</c> this handler answers to (<see cref="OutboxMessageTypes"/>).</summary>
    string MessageType { get; }

    Task HandleAsync(string payloadJson, CancellationToken ct);
}

/// <summary>
/// Deserializes the payload once, here, so no handler repeats it. The stored JSON was written by this same
/// assembly, so a payload that will not parse is a bug or a corrupted row — either way it belongs in the
/// outbox's retry-and-log path, not in a silent null.
/// </summary>
public abstract class OutboxMessageHandler<TPayload> : IOutboxMessageHandler
{
    public abstract string MessageType { get; }

    public Task HandleAsync(string payloadJson, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(payloadJson)
            ?? throw new InvalidOperationException(
                $"Outbox message of type '{MessageType}' has an unreadable payload.");

        return HandleAsync(payload, ct);
    }

    protected abstract Task HandleAsync(TPayload payload, CancellationToken ct);
}
