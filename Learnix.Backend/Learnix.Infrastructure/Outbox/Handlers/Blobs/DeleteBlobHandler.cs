using Learnix.Application.Common.Abstractions.Storage;

namespace Learnix.Infrastructure.Outbox.Handlers.Blobs;

/// <summary>
/// Reaps a blob that an entity let go of — a replaced avatar, a removed cover, a released lesson video.
/// The outbox never confirms an upload, only deletes one (ADR-BLOB-003).
/// </summary>
internal sealed class DeleteBlobHandler(IBlobStorageService blobStorage)
    : OutboxMessageHandler<DeleteBlobPayload>
{
    public override string MessageType => OutboxMessageTypes.DeleteBlob;

    protected override Task HandleAsync(DeleteBlobPayload payload, CancellationToken ct) =>
        blobStorage.DeleteAsync(payload.BlobPath, ct);
}
