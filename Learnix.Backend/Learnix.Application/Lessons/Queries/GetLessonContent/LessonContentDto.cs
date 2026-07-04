using Learnix.Domain.Enums;

namespace Learnix.Application.Lessons.Queries.GetLessonContent;

public sealed record LessonContentDto(
    Guid LessonId,
    string Title,
    LessonType LessonType,
    // Video lesson fields
    string? VideoUrl,
    string? Description,
    int? DurationSeconds,
    // Post lesson fields
    string? Content);
