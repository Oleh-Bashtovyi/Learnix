using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.UnarchiveCourse;

public sealed record UnarchiveCourseCommand(Guid CourseId) : IRequest<Result>;
