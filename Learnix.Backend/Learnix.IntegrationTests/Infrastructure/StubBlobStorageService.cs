using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;

namespace Learnix.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for Azure Blob Storage. Category CRUD without an image never touches it; the image path is
/// covered separately. <see cref="CommitUploadAsync"/> echoes the temp path back as if promoted, so a
/// test that does exercise the image branch sees a plausible permanent path without a real container.
/// </summary>
internal sealed class StubBlobStorageService : IBlobStorageService
{
    public Task<UploadUrlResponse> GenerateUploadUrlAsync(UploadTarget target, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult(new UploadUrlResponse($"https://stub/upload/{Guid.NewGuid():N}", $"temp-uploads/{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddMinutes(15)));

    public Task<Result<BlobMetadata>> CommitUploadAsync(string tempBlobPath, UploadTarget target, CancellationToken cancellationToken)
    {
        var name = tempBlobPath.Split('/')[^1];
        return Task.FromResult(Result.Ok(new BlobMetadata($"category-images/{name}", "image/webp", 1024)));
    }

    public string GenerateReadUrl(string blobPath, TimeSpan ttl) => $"https://stub/read/{blobPath}";

    public string GetPublicUrl(string blobPath) => $"https://stub/public/{blobPath}";

    public Task DeleteAsync(string blobPath, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken) => Task.CompletedTask;
}
