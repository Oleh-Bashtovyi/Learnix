using System.Text.Json;
using Learnix.Application.Common.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Outbox;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}

internal sealed class OutboxMessageDispatcher(
    IBlobStorageService blobStorage,
    ILogger<OutboxMessageDispatcher> logger
) : IOutboxMessageDispatcher
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case OutboxMessageTypes.DeleteBlob:
                var deletePayload = JsonSerializer.Deserialize<DeleteBlobPayload>(message.Payload)!;
                await blobStorage.DeleteAsync(deletePayload.BlobPath, ct);
                break;



            default:
                logger.LogError("Unknown outbox message type: {Type}", message.Type);
                throw new InvalidOperationException($"Unknown message type: {message.Type}");
        }
    }
}
