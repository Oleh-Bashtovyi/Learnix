using FluentResults;

namespace Learnix.Application.Common.Abstractions.Storage;

public interface IBlobStorageService
{
    /// <summary>
    /// Generates a SAS URL for client to upload directly to blob storage.
    /// The blob is created without 'confirmed' tag and will be removed by 
    /// lifecycle policy unless MarkConfirmedAsync is called within TTL.
    /// </summary>
    Task<UploadUrlResponse> GenerateUploadUrlAsync(
        UploadTarget target,
        string contentType,
        CancellationToken ct);

    /// <summary>
    /// Validates uploaded blob: existence, size, magic bytes.
    /// Does NOT modify the blob. Call this synchronously in command handlers
    /// before saving entity changes to DB.
    /// </summary>
    Task<Result<BlobMetadata>> ValidateAsync(
        string blobPath,
        UploadTarget target,
        CancellationToken ct);

    /// <summary>
    /// Marks a blob as confirmed (sets tag). Called only from outbox dispatcher
    /// after entity is successfully persisted. Idempotent.
    /// </summary>
    Task MarkConfirmedAsync(string blobPath, CancellationToken ct);

    /// <summary>
    /// Generates a read SAS URL for a confirmed blob.
    /// </summary>
    string GenerateReadUrl(string blobPath, TimeSpan ttl);

    /// <summary>
    /// Returns a stable public URL for blobs in public containers (avatars, covers, category images).
    /// Do NOT use for protected content (videos, certificates).
    /// </summary>
    string GetPublicUrl(string blobPath);

    /// <summary>
    /// Deletes a blob. Safe to call on non-existent blobs.
    /// Errors are logged, not thrown.
    /// </summary>
    Task DeleteAsync(string blobPath, CancellationToken ct);

    /// <summary>
    /// Server-side direct upload. Used by background services (e.g. certificate PDF generation).
    /// </summary>
    Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken ct);

}

public record UploadUrlResponse(
    string UploadUrl,
    string BlobPath,
    DateTimeOffset ExpiresAt);

public record BlobMetadata(
    string BlobPath,
    string ContentType,
    long SizeBytes);

public enum UploadTarget
{
    Avatar,
    CourseCover,
    LessonVideo,
    Certificate,
    CategoryImage
}
