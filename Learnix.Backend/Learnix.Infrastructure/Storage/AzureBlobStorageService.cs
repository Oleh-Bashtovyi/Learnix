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

// Containers (names come from BlobStorageOptions; the defaults are shown):
//
//   temp-uploads/     ← every upload lands here first, via a SAS URL, and is promoted on commit
//   avatars/
//   course-covers/
//   course-videos/
//   certificates/
//   category-images/
//
// Blobs are flat inside a container: the name is a bare {guid:N}, with no folders and no extension.
// The type is carried by the Content-Type header, which CommitUploadAsync overwrites with the value
// sniffed from the magic bytes rather than trusting what the client declared.
// Entities store the relative "{container}/{blobName}" path — the container prefix is mandatory
// (ADR-BLOB-003), and ParseBlobPath below splits it back out.

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

    /// <summary>
    /// The whitelist below and <see cref="DetectMimeFromMagicBytes"/> have to agree on the exact strings:
    /// the detector's return value is looked up in the whitelist, so a typo on either side would silently
    /// reject a valid file. Naming them once is what keeps the two sides in step.
    /// </summary>
    private static class MimeTypes
    {
        public const string Jpeg = "image/jpeg";
        public const string Png = "image/png";
        public const string Webp = "image/webp";
        public const string Mp4 = "video/mp4";
        public const string Webm = "video/webm";
        public const string Pdf = "application/pdf";
        public const string Unknown = "application/octet-stream";
    }

    private static readonly Dictionary<UploadTarget, HashSet<string>> AllowedContentTypes = new()
    {
        [UploadTarget.Avatar] = [MimeTypes.Jpeg, MimeTypes.Png, MimeTypes.Webp],
        [UploadTarget.CourseCover] = [MimeTypes.Jpeg, MimeTypes.Png, MimeTypes.Webp],
        [UploadTarget.LessonVideo] = [MimeTypes.Mp4, MimeTypes.Webm],
        [UploadTarget.Certificate] = [MimeTypes.Pdf],
        [UploadTarget.CategoryImage] = [MimeTypes.Jpeg, MimeTypes.Png, MimeTypes.Webp],
    };

    public Task<UploadUrlResponse> GenerateUploadUrlAsync(
        UploadTarget target,
        string contentType,
        CancellationToken cancellationToken)
    {
        var containerName = _options.TempContainer;
        var blobName = $"{Guid.NewGuid():N}";
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

    public async Task<Result<BlobMetadata>> CommitUploadAsync(
        string tempBlobPath,
        UploadTarget target,
        CancellationToken cancellationToken)
    {
        var (tempContainer, tempBlobName) = ParseBlobPath(tempBlobPath);
        if (tempContainer != _options.TempContainer)
            return Result.Fail(new BlobValidationError("Invalid temporary blob path."));

        var tempBlob = blobServiceClient
            .GetBlobContainerClient(tempContainer)
            .GetBlobClient(tempBlobName);

        if (!await tempBlob.ExistsAsync(cancellationToken))
            return Result.Fail(new NotFoundError($"File not found or expired. Please upload it again."));

        var properties = await tempBlob.GetPropertiesAsync(cancellationToken: cancellationToken);
        var size = properties.Value.ContentLength;

        var maxSize = MaxSizes[target];
        if (size > maxSize)
        {
            await tempBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return Result.Fail(new BlobValidationError(
                $"File too large. Size: {FormatBytes(size)}, max: {FormatBytes(maxSize)}"));
        }

        var actualContentType = await DetectContentTypeAsync(tempBlob, cancellationToken);
        if (!AllowedContentTypes[target].Contains(actualContentType))
        {
            await tempBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return Result.Fail(new BlobValidationError(
                $"Content type '{actualContentType}' not allowed for {target}"));
        }

        var (finalContainer, finalBlobName) = BuildBlobLocation(target);
        var finalBlob = blobServiceClient
            .GetBlobContainerClient(finalContainer)
            .GetBlobClient(finalBlobName);

        var copyOp = await finalBlob.StartCopyFromUriAsync(tempBlob.Uri, cancellationToken: cancellationToken);
        await copyOp.WaitForCompletionAsync(cancellationToken);

        await tempBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        // Overwrite Content-Type header with trusted value (in case client lied)
        await finalBlob.SetHttpHeadersAsync(
            new BlobHttpHeaders { ContentType = actualContentType },
            cancellationToken: cancellationToken);

        return Result.Ok(new BlobMetadata($"{finalContainer}/{finalBlobName}", actualContentType, size));
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

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken)
    {
        try
        {
            var (container, blobName) = ParseBlobPath(blobPath);
            await blobServiceClient
                .GetBlobContainerClient(container)
                .GetBlobClient(blobName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob {BlobPath}", blobPath);
        }
    }

    public async Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var (container, blobName) = ParseBlobPath(blobPath);
        var blob = blobServiceClient
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);
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

    private static async Task<string> DetectContentTypeAsync(BlobClient blob, CancellationToken cancellationToken)
    {
        var range = new HttpRange(0, 512);
        var response = await blob.DownloadStreamingAsync(
            new BlobDownloadOptions { Range = range }, cancellationToken);

        using var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms, cancellationToken);
        return DetectMimeFromMagicBytes(ms.ToArray());
    }

    private static string DetectMimeFromMagicBytes(byte[] bytes)
    {
        if (bytes.Length < 4) return MimeTypes.Unknown;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return MimeTypes.Jpeg;

        // PNG: 89 50 4E 47
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return MimeTypes.Png;

        // WebP: RIFF....WEBP
        if (bytes.Length >= 12
            && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return MimeTypes.Webp;

        // MP4: ?? ?? ?? ?? 66 74 79 70 (ftyp box at offset 4)
        if (bytes.Length >= 8
            && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
            return MimeTypes.Mp4;

        // WebM: 1A 45 DF A3
        if (bytes[0] == 0x1A && bytes[1] == 0x45 && bytes[2] == 0xDF && bytes[3] == 0xA3)
            return MimeTypes.Webm;

        // PDF: 25 50 44 46
        if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
            return MimeTypes.Pdf;

        return MimeTypes.Unknown;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblSByte = bytes;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblSByte = bytes / 1024.0;
        }

        return $"{dblSByte:0.##} {suffix[i]}";
    }
}
