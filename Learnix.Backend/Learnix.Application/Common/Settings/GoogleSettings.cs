namespace Learnix.Application.Common.Settings;

public sealed class GoogleSettings
{
    /// <summary>OAuth Client ID from Google Cloud Console. Used as audience when validating ID tokens.</summary>
    public string ClientId { get; init; } = null!;
}
