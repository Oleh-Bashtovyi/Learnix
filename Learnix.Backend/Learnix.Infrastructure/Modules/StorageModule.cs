using Azure.Storage.Blobs;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>Azure Blob Storage: the client and the SAS/commit service behind IBlobStorageService.</summary>
public static class StorageModule
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("AzureBlobStorage");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("AzureBlobStorage connection string is missing or empty.");
            }

            return new BlobServiceClient(connectionString);
        });

        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}
