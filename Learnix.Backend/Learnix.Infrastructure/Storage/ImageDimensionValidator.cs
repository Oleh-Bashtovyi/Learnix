namespace Learnix.Infrastructure.Storage;

/// <summary>
/// The width/height/aspect comparison behind <see cref="AzureBlobStorageService"/>'s per-target image
/// checks, split out as a pure function — no <c>BlobClient</c>, no decoded image — so it can be
/// unit-tested directly. See ADR-BACK-BLOB-005.
/// </summary>
internal static class ImageDimensionValidator
{
    internal readonly record struct Rule(int MinWidth, int MinHeight, double Aspect);

    /// <summary>2% slack on <see cref="Rule.Aspect"/> — the cropper renders the crop to an exact pixel
    /// size, so a legitimate upload's aspect matches to floating-point precision; the slack exists only
    /// so this check isn't more brittle than the byte-for-byte checks around it.</summary>
    internal const double AspectTolerance = 0.02;

    /// <summary>Returns null when <paramref name="width"/>/<paramref name="height"/> satisfy
    /// <paramref name="rule"/>, otherwise the message to fail the commit with.</summary>
    internal static string? Validate(int width, int height, Rule rule)
    {
        if (width < rule.MinWidth || height < rule.MinHeight)
        {
            return $"Image is {width}x{height}px, smaller than the {rule.MinWidth}x{rule.MinHeight}px minimum.";
        }

        var aspect = (double)width / height;
        if (Math.Abs(aspect - rule.Aspect) > AspectTolerance)
        {
            return $"Image aspect ratio {aspect:0.###} does not match the expected {rule.Aspect:0.###}.";
        }

        return null;
    }
}
