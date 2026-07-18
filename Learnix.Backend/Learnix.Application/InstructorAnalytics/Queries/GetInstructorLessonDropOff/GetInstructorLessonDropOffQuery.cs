using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorLessonDropOff;

public sealed record GetInstructorLessonDropOffQuery(Guid CourseId)
    : IRequest<Result<LessonDropOffDto>>;

/// <summary>
/// Per-lesson completion for a single course, in curriculum order — the "where do students drop off"
/// curve. <paramref name="Enrolled"/> is the shared denominator for every lesson's completion rate.
/// </summary>
public sealed record LessonDropOffDto(int Enrolled, List<LessonDropOffItemDto> Lessons);

public sealed record LessonDropOffItemDto(Guid LessonId, string LessonTitle, int Completed);
