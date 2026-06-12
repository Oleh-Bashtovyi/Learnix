namespace Learnix.Application.Common.Constants;

public static class BlobUrlTtlConstants
{
    public static readonly TimeSpan VideoLessonReadUrl = TimeSpan.FromHours(2);
    public static readonly TimeSpan CertificateReadUrl = TimeSpan.FromHours(24);
}
