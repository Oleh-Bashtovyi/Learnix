using Learnix.Domain.Enums;
using Learnix.Domain.Events.Lessons;

namespace Learnix.Domain.Entities;

public class VideoLesson : Lesson
{
    private VideoLesson() { }

    private VideoLesson(
        Guid sectionId,
        string title,
        int order,
        string videoBlobPath,
        string? description,
        int? durationSeconds)
        : base(sectionId, title, order, LessonType.Video)
    {
        VideoBlobPath = videoBlobPath;
        Description = description;
        DurationSeconds = durationSeconds;

        if (!string.IsNullOrWhiteSpace(videoBlobPath))
            RaiseDomainEvent(new LessonVideoAttachedDomainEvent(Id, videoBlobPath));
    }

    public string VideoBlobPath { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? DurationSeconds { get; private set; }

    public static VideoLesson Create(
        Guid sectionId,
        string title,
        int order,
        string videoBlobPath,
        string? description = null,
        int? durationSeconds = null)
        => new(sectionId, title, order, videoBlobPath, description, durationSeconds);

    public override bool IsPublishReady() => !string.IsNullOrWhiteSpace(VideoBlobPath);

    public void ReplaceVideo(string newBlobPath)
    {
        if (newBlobPath == VideoBlobPath) return;

        var oldPath = VideoBlobPath;
        VideoBlobPath = newBlobPath;

        RaiseDomainEvent(new LessonVideoReleasedDomainEvent(Id, oldPath));

        if (!string.IsNullOrWhiteSpace(VideoBlobPath))
            RaiseDomainEvent(new LessonVideoAttachedDomainEvent(Id, newBlobPath));
    }

    public void UpdateMetadata(string title, string? description, int? durationSeconds)
    {
        UpdateTitle(title);
        Description = description;
        DurationSeconds = durationSeconds;
    }

    public override void PrepareForDeletion()
    {
        if (!string.IsNullOrWhiteSpace(VideoBlobPath))
            RaiseDomainEvent(new LessonVideoReleasedDomainEvent(Id, VideoBlobPath));
    }
}
