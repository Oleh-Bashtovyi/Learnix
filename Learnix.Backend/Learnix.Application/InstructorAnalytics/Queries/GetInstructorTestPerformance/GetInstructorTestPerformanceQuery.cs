using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorTestPerformance;

public sealed record GetInstructorTestPerformanceQuery(Guid? CourseId = null)
    : IRequest<Result<List<InstructorTestPerformanceDto>>>;

public sealed record InstructorTestPerformanceDto(
    Guid CourseId,
    string CourseTitle,
    Guid LessonId,
    string LessonTitle,
    double AverageScore,
    double MaxScore,
    double PassRate,
    int AttemptsCount);
