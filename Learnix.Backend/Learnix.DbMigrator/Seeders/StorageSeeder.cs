using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Learnix.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Learnix.DbMigrator.Seeders;

internal sealed class StorageSeeder(
    BlobServiceClient blobServiceClient,
    ILogger<StorageSeeder> logger
)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // IMPORTANT: This seeder is used ONLY for local development (Azurite).
        // It provides a "Zero-Click Setup" for developers when running locally.
        // All Production containers, access tiers (Public/Private), and Lifecycle Policies
        // are managed exclusively via Terraform (infrastructure/storage.tf).
        // Azurite does not support Advanced Azure features like Lifecycle Policies,
        // which is why they are not configured here.

        // Both the names and the access levels come from BlobContainers, which is the same source
        // Terraform is held to by `npm run check:containers` — so local Azurite and the provisioned
        // account cannot disagree about which containers exist or which of them are readable anonymously.
        foreach (var name in BlobContainers.All)
        {
            var container = blobServiceClient.GetBlobContainerClient(name);
            var accessType = BlobContainers.IsPublic(name)
                ? PublicAccessType.Blob
                : PublicAccessType.None;

            var response = await container.CreateIfNotExistsAsync(accessType, cancellationToken: cancellationToken);

            if (response is not null)
                logger.LogInformation("Created blob container: {Container} with access {Access}", name, accessType);
        }

        // Configure local host CORS
        var blobProperties = await blobServiceClient.GetPropertiesAsync(cancellationToken);

        blobProperties.Value.Cors =
        [
            new()
            {
                AllowedOrigins = "http://localhost:5173",
                AllowedMethods = "GET,PUT,POST,DELETE,OPTIONS",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600,
            }
        ];

        await blobServiceClient.SetPropertiesAsync(blobProperties.Value, cancellationToken);

        logger.LogInformation("Blob Storage initialization completed successfully.");
    }
}
