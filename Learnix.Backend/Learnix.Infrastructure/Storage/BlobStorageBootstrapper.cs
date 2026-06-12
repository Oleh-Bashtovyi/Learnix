using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Storage;

internal sealed class BlobStorageBootstrapper(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<BlobStorageBootstrapper> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var containers = new[]
        {
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

        // Configure local host. TODO: do something with it
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
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
