using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.DbMigrator.Seeders;

public sealed class CategorySeeder(
    IServiceProvider serviceProvider,
    ILogger<CategorySeeder> logger) : IDataSeeder
{
    private static readonly (string Name, string Slug, string ImageName, string ContentType)[] SeedCategories =
    [
        ("Programming", "programming", "category_programming.webp", "image/webp"),
        ("Web Development", "web-development", "category_web-development.webp", "image/webp"),
        ("Data Science", "data-science", "category_data-science.webp", "image/webp"),
        ("Design", "design", "category_design.webp", "image/webp"),
        ("Business", "business", "category_business.webp", "image/webp"),
        ("Marketing", "marketing", "category_marketing.webp", "image/webp"),
        ("Personal Development", "personal-development", "category_personal-development.webp", "image/webp"),
        ("Language Learning", "language-learning", "category_language-learning.webp", "image/webp"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var blobOptions = scope.ServiceProvider.GetRequiredService<IOptions<BlobStorageOptions>>();

        var existing = await context.Categories
            .ToDictionaryAsync(c => c.Slug, cancellationToken);

        var inserted = 0;
        var imaged = 0;

        foreach (var seed in SeedCategories)
        {
            if (!existing.TryGetValue(seed.Slug, out var category))
            {
                category = Category.CreateSystem(seed.Name, seed.Slug);
                context.Categories.Add(category);
                inserted++;
            }
            // A category that already carries an image keeps it: an admin may have replaced it
            // through the UI, and a re-run of the seeder must not overwrite that.
            else if (category.ImageBlobPath is not null)
            {
                continue;
            }

            var blobPath = await UploadImageAsync(
                seed.ImageName, seed.ContentType, blobStorage, blobOptions, cancellationToken);

            if (blobPath is null)
                continue;

            category.SetImage(blobPath);
            imaged++;
        }

        if (inserted == 0 && imaged == 0)
        {
            logger.LogInformation("Category seed: nothing to add.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Category seed: inserted {Inserted} system categories, attached {Imaged} images.",
            inserted, imaged);
    }

    private async Task<string?> UploadImageAsync(
        string imageName,
        string contentType,
        IBlobStorageService blobStorage,
        IOptions<BlobStorageOptions> blobOptions,
        CancellationToken cancellationToken)
    {
        // The container prefix is part of the stored path by contract — everything downstream
        // parses it back out to resolve the container (ADR-BACK-BLOB-002).
        var blobPath = $"{blobOptions.Value.CategoryImageContainer}/{Guid.NewGuid()}-{imageName}";

        try
        {
            var assembly = typeof(CategorySeeder).Assembly;
            await using var stream = assembly.GetManifestResourceStream(
                $"Learnix.DbMigrator.Assets.{imageName}");

            if (stream is null)
            {
                logger.LogWarning("Category image {Image} is not an embedded resource.", imageName);
                return null;
            }

            await blobStorage.UploadAsync(blobPath, stream, contentType, cancellationToken);
            return blobPath;
        }
        catch (Exception ex)
        {
            // A missing picture is not worth failing the migration over — the category still
            // seeds, and the client falls back to its bundled placeholder.
            logger.LogWarning(ex, "Failed to upload category image {Image}.", imageName);
            return null;
        }
    }
}
