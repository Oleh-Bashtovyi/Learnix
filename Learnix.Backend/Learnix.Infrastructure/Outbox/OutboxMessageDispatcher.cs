using System.Reflection;
using Learnix.Infrastructure.Outbox.Handlers;

namespace Learnix.Infrastructure.Outbox;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Routes a message to the handler that owns its type. The lookup replaces the switch the processor used to
/// carry: adding a message type now means adding a class, not editing a background service (ADR-INFRA-013).
/// </summary>
internal sealed class OutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly Dictionary<string, IOutboxMessageHandler> _handlers;

    public OutboxMessageDispatcher(IEnumerable<IOutboxMessageHandler> handlers)
    {
        _handlers = [];

        // S3267: the `if` below is a guard that throws, not a filter — there is nothing to move into Where.
#pragma warning disable S3267
        foreach (var handler in handlers)
        {
            // Two handlers for one type is not a routing problem to resolve at runtime — it is a mistake, and
            // it surfaces at startup rather than as one of them quietly never running.
            if (!_handlers.TryAdd(handler.MessageType, handler))
            {
                throw new InvalidOperationException(
                    $"Two outbox handlers claim the message type '{handler.MessageType}': " +
                    $"{_handlers[handler.MessageType].GetType().Name} and {handler.GetType().Name}.");
            }
        }
#pragma warning restore S3267

        EnsureEveryMessageTypeIsHandled();
    }

    /// <summary>
    /// A message type nobody handles is a side-effect that is enqueued, retried, and never happens. The switch
    /// this replaced had no way of saying so; a set difference does. Better to refuse to start than to lose
    /// somebody's password reset to a `default:` branch at three in the morning.
    /// </summary>
    private void EnsureEveryMessageTypeIsHandled()
    {
        var declared = typeof(OutboxMessageTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        var unhandled = declared.Where(type => !_handlers.ContainsKey(type)).ToList();

        if (unhandled.Count > 0)
        {
            throw new InvalidOperationException(
                "No outbox handler is registered for these message types: " + string.Join(", ", unhandled));
        }
    }

    public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(message.Type, out var handler))
        {
            // Throw rather than skip: an unhandled type means a message was enqueued that nobody delivers.
            // The processor's retry-and-log path is where that must become visible.
            throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
        }

        return handler.HandleAsync(message.Payload, cancellationToken);
    }
}
