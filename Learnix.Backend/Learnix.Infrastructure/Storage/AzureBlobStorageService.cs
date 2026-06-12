using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Storage;

//Containers:
//├── course-videos/
//│   └── courses/{courseId}/ lessons /{ lessonId}/{ uploadId}.mp4
//├── course-covers/
//│   └── courses/{courseId}/{ uploadId}.{ ext}
//├── avatars /
//│   └── users /{ userId}/{ uploadId}.{ ext}
//└── certificates /
//    └── { uniqueCode}.pdf

internal sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger
) : IBlobStorageService
{
    private readonly BlobStorageOptions _options = options.Value;

    private static readonly Dictionary<UploadTarget, long> MaxSizes = new()
    {
        [UploadTarget.Avatar] = 5L * 1024 * 1024,                // 5 MB
        [UploadTarget.CourseCover] = 10L * 1024 * 1024,          // 10 MB
        [UploadTarget.LessonVideo] = 2L * 1024 * 1024 * 1024,    // 2 GB
        [UploadTarget.Certificate] = 5L * 1024 * 1024,           // 5 MB
        [UploadTarget.CategoryImage] = 2L * 1024 * 1024,         // 2 MB
    };

    private static readonly Dictionary<UploadTarget, HashSet<string>> AllowedContentTypes = new()
    {
        [UploadTarget.Avatar] = ["image/jpeg", "image/png", "image/webp"],
        [UploadTarget.CourseCover] = ["image/jpeg", "image/png", "image/webp"],
        [UploadTarget.LessonVideo] = ["video/mp4", "video/webm"],
        [UploadTarget.Certificate] = ["application/pdf"],
        [UploadTarget.CategoryImage] = ["image/jpeg", "image/png", "image/webp"],
    };

    public Task<UploadUrlResponse> GenerateUploadUrlAsync(
        UploadTarget target,
        string contentType,
        CancellationToken ct)
    {
        var (containerName, blobName) = BuildBlobLocation(target);
        var blob = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create);

        var sasUri = blob.GenerateSasUri(sasBuilder);

        return Task.FromResult(new UploadUrlResponse(
            UploadUrl: sasUri.ToString(),
            BlobPath: $"{containerName}/{blobName}",
            ExpiresAt: sasBuilder.ExpiresOn));
    }

    public async Task<Result<BlobMetadata>> ValidateAsync(
        string blobPath,
        UploadTarget target,
        CancellationToken ct)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        var blob = blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        if (!await blob.ExistsAsync(ct))
            return Result.Fail(new NotFoundError($"Blob not found: {blobPath}"));

        var properties = await blob.GetPropertiesAsync(cancellationToken: ct);
        var size = properties.Value.ContentLength;

        var maxSize = MaxSizes[target];
        if (size > maxSize)
        {
            await blob.DeleteIfExistsAsync(cancellationToken: ct);
            return Result.Fail(new BlobValidationError(
                $"File too large. Size: {size} bytes, max: {maxSize} bytes"));
        }

        var actualContentType = await DetectContentTypeAsync(blob, ct);
        if (!AllowedContentTypes[target].Contains(actualContentType))
        {
            await blob.DeleteIfExistsAsync(cancellationToken: ct);
            return Result.Fail(new BlobValidationError(
                $"Content type '{actualContentType}' not allowed for {target}"));
        }

        // Overwrite Content-Type header with trusted value (in case client lied)
        await blob.SetHttpHeadersAsync(
            new BlobHttpHeaders { ContentType = actualContentType },
            cancellationToken: ct);

        return Result.Ok(new BlobMetadata(blobPath, actualContentType, size));
    }

    public async Task MarkConfirmedAsync(string blobPath, CancellationToken ct)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        var blob = blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        // Idempotent: setting same tag twice is a no-op in Azure
        await blob.SetTagsAsync(
            new Dictionary<string, string> { ["confirmed"] = "true" },
            cancellationToken: ct);
    }

    public string GenerateReadUrl(string blobPath, TimeSpan ttl)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        var blob = blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(ttl),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blob.GenerateSasUri(sasBuilder).ToString();
    }

    public string GetPublicUrl(string blobPath)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        return blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName)
            .Uri.ToString();
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct)
    {
        try
        {
            var (container, blobName) = ParseBlobPath(blobPath);
            await blobServiceClient
                .GetBlobContainerClient(container)
                .GetBlobClient(blobName)
                .DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob {BlobPath}", blobPath);
        }
    }

    public async Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken ct)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        var blob = blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
    }

    private (string container, string blobName) BuildBlobLocation(UploadTarget target)
    {
        var container = target switch
        {
            UploadTarget.Avatar => _options.AvatarContainer,
            UploadTarget.CourseCover => _options.CourseCoverContainer,
            UploadTarget.LessonVideo => _options.LessonVideoContainer,
            UploadTarget.Certificate => _options.CertificateContainer,
            UploadTarget.CategoryImage => _options.CategoryImageContainer,
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        var blobName = $"{Guid.NewGuid():N}";
        return (container, blobName);
    }

    private static (string container, string blobName) ParseBlobPath(string blobPath)
    {
        var slashIndex = blobPath.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == blobPath.Length - 1)
            throw new ArgumentException($"Invalid blob path format: {blobPath}", nameof(blobPath));

        return (
            container: blobPath[..slashIndex],
            blobName: blobPath[(slashIndex + 1)..]
        );
    }

    private static async Task<string> DetectContentTypeAsync(BlobClient blob, CancellationToken ct)
    {
        var range = new HttpRange(0, 512);
        var response = await blob.DownloadStreamingAsync(
            new BlobDownloadOptions { Range = range }, ct);

        using var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms, ct);
        return DetectMimeFromMagicBytes(ms.ToArray());
    }

    private static string DetectMimeFromMagicBytes(byte[] bytes)
    {
        if (bytes.Length < 4) return "application/octet-stream";

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";

        // WebP: RIFF....WEBP
        if (bytes.Length >= 12
            && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return "image/webp";

        // MP4: ?? ?? ?? ?? 66 74 79 70 (ftyp box at offset 4)
        if (bytes.Length >= 8
            && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
            return "video/mp4";

        // WebM: 1A 45 DF A3
        if (bytes[0] == 0x1A && bytes[1] == 0x45 && bytes[2] == 0xDF && bytes[3] == 0xA3)
            return "video/webm";

        // PDF: 25 50 44 46
        if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
            return "application/pdf";

        return "application/octet-stream";
    }
}
