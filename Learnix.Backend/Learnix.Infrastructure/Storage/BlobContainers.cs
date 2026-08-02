namespace Learnix.Infrastructure.Storage;

public enum BlobContainerAccess
{
    /// <summary>Anonymous read. A plain URL from <c>GetPublicUrl</c> resolves forever.</summary>
    Public,

    /// <summary>No anonymous read. Only reachable through a SAS from <c>GenerateReadUrl</c>.</summary>
    Private
}

/// <summary>
/// The container names, and whether each one answers anonymous reads.
/// </summary>
/// <remarks>
/// These are constants rather than configuration because they cannot be configured: entities persist the
/// container as part of their blob path (ADR-BACK-BLOB-002), so a rename is a data migration across five
/// columns plus a physical move of the blobs — not a setting change. As `appsettings.json` values they
/// invited exactly the edit that silently splits the data: new uploads land in the new container while
/// every stored row still points at the old one, and the application starts up clean.
///
/// <see cref="Access"/> is load-bearing, not documentation. A SAS with an expiry is worth nothing if the
/// container answers anonymous reads anyway — which is how `course-videos` was once provisioned public
/// while the code assumed otherwise. `npm run check:containers` holds this file and
/// `infrastructure/storage.tf` to the same answer, because Terraform is what actually creates the
/// containers in production and it cannot read C#.
///
/// Related ADRs:
/// - ADR-BACK-BLOB-002: relative `{container}/{blobName}` paths in the database
/// </remarks>
public static class BlobContainers
{
    /// <summary>Every upload lands here first via a write-only SAS, and is promoted on commit.</summary>
    public const string Temp = "temp-uploads";

    public const string Avatars = "avatars";
    public const string CourseCovers = "course-covers";
    public const string CourseVideos = "course-videos";
    public const string Certificates = "certificates";
    public const string CategoryImages = "category-images";

    /// <summary>Verified against <c>infrastructure/storage.tf</c> by <c>npm run check:containers</c>.</summary>
    public static readonly IReadOnlyDictionary<string, BlobContainerAccess> Access =
        new Dictionary<string, BlobContainerAccess>
        {
            [Avatars] = BlobContainerAccess.Public,
            [CourseCovers] = BlobContainerAccess.Public,
            [CategoryImages] = BlobContainerAccess.Public,
            [CourseVideos] = BlobContainerAccess.Private,
            [Certificates] = BlobContainerAccess.Private,
            [Temp] = BlobContainerAccess.Private,
        };

    public static IReadOnlyCollection<string> All => (IReadOnlyCollection<string>)Access.Keys;

    public static bool IsPublic(string container) =>
        Access.TryGetValue(container, out var access) && access == BlobContainerAccess.Public;
}
