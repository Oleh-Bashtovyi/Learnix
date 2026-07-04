namespace Learnix.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public string TempContainer { get; set; } = "temp-uploads";
    public string AvatarContainer { get; set; } = "avatars";
    public string CourseCoverContainer { get; set; } = "course-covers";
    public string LessonVideoContainer { get; set; } = "course-videos";
    public string CertificateContainer { get; set; } = "certificates";
    public string CategoryImageContainer { get; set; } = "category-images";
}
