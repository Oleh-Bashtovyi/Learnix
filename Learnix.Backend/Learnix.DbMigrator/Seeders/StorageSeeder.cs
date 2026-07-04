using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Learnix.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.DbMigrator.Seeders;

internal sealed class StorageSeeder(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<StorageSeeder> logger
)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // IMPORTANT: This seeder is used ONLY for local development (Azurite).
        // It provides a "Zero-Click Setup" for developers when running locally.
        // All Production containers, access tiers (Public/Private), and Lifecycle Policies
        // are managed exclusively via Terraform (infrastructure/storage.tf).
        // Azurite does not support Advanced Azure features like Lifecycle Policies,
        // which is why they are not configured here.

        var containers = new[]
        {
            options.Value.TempContainer,
            options.Value.AvatarContainer,
            options.Value.CourseCoverContainer,
            options.Value.LessonVideoContainer,
            options.Value.CertificateContainer,
            options.Value.CategoryImageContainer,
        };

        var publicContainers = new HashSet<string>
        {
            options.Value.AvatarContainer,
            options.Value.CourseCoverContainer,
            options.Value.CategoryImageContainer
        };

        foreach (var name in containers)
        {
            var container = blobServiceClient.GetBlobContainerClient(name);
            var accessType = publicContainers.Contains(name)
                ? PublicAccessType.Blob
                : PublicAccessType.None;

            var response = await container.CreateIfNotExistsAsync(accessType, cancellationToken: ct);

            if (response is not null)
                logger.LogInformation("Created blob container: {Container} with access {Access}", name, accessType);
        }

        // Configure local host CORS
        var blobProperties = await blobServiceClient.GetPropertiesAsync(ct);

        blobProperties.Value.Cors = new List<BlobCorsRule>
        {
            new()
            {
                AllowedOrigins = "http://localhost:5173",
                AllowedMethods = "GET,PUT,POST,DELETE,OPTIONS",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600,
            }
        };

        await blobServiceClient.SetPropertiesAsync(blobProperties.Value, ct);

        logger.LogInformation("Blob Storage initialization completed successfully.");
    }
}
