namespace Learnix.Application.Common.Options;

public class AppOptions
{
    // Required for links generation in emails: {ClientBaseUrl}/verify-email?userId=...&token=...
    public string ClientBaseUrl { get; init; } = null!;  // e.g. "http://localhost:5173"

    // Overrides the scheme/host/port of every blob URL (SAS and public) returned to callers.
    // Needed only when ConnectionStrings:AzureBlobStorage points at an address unreachable from
    // outside the server — e.g. the "azurite" Docker Compose hostname, which resolves only inside
    // the Docker network, not from a browser on the host. Path and query (including the SAS
    // token) are left untouched. Unset in Development (host-run) and Production, where the blob
    // endpoint is already reachable by whoever receives the URL.
    public string? PublicBlobBaseUrl { get; init; }
}
