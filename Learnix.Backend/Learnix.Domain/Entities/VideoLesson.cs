using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class VideoLesson : Lesson
{
    private VideoLesson() { }

    private VideoLesson(
        Guid sectionId,
        string title,
        int order,
        string videoUrl,
        string? description,
        int? durationSeconds)
        : base(sectionId, title, order, LessonType.Video)
    {
        VideoUrl = videoUrl;
        Description = description;
        DurationSeconds = durationSeconds;
    }

    public string VideoUrl { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? DurationSeconds { get; private set; }

    internal static VideoLesson Create(
        Guid sectionId,
        string title,
        int order,
        string videoUrl,
        string? description = null,
        int? durationSeconds = null)
        => new(sectionId, title, order, videoUrl, description, durationSeconds);

    internal void UpdateVideo(string title, string videoUrl, string? description, int? durationSeconds)
    {
        UpdateTitle(title);
        VideoUrl = videoUrl;
        Description = description;
        DurationSeconds = durationSeconds;
    }
}
