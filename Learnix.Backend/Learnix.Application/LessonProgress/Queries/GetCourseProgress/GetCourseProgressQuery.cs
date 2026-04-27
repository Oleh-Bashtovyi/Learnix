using FluentResults;
using MediatR;

namespace Learnix.Application.LessonProgress.Queries.GetCourseProgress;

public sealed record GetCourseProgressQuery(Guid CourseId)
    : IRequest<Result<CourseProgressResponse>>;
