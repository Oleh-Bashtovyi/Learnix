using FluentResults;

namespace Learnix.Application.Common.Abstractions.Storage;

public interface IBlobStorageService
{
    /// <summary>
    /// Generates a SAS URL for client to upload directly to a temporary blob container.
    /// The blob will be removed by an Azure lifecycle policy after 24 hours 
    /// unless CommitUploadAsync is called to move it to its permanent location.
    /// </summary>
    Task<UploadUrlResponse> GenerateUploadUrlAsync(
        UploadTarget target,
        string contentType,
        CancellationToken ct);

    /// <summary>
    /// Validates an uploaded blob (size, magic bytes) in the temporary container,
    /// moves it to the final destination container, and deletes the temporary blob.
    /// Returns the new permanent blob path.
    /// Call this synchronously in command handlers before saving entity changes to DB.
    /// </summary>
    Task<Result<BlobMetadata>> CommitUploadAsync(
        string tempBlobPath,
        UploadTarget target,
        CancellationToken ct);

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
