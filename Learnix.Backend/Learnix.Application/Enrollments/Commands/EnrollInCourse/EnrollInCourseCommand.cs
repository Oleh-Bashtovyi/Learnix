using FluentResults;
using MediatR;

namespace Learnix.Application.Enrollments.Commands.EnrollInCourse;

public sealed record EnrollInCourseCommand(Guid CourseId) : IRequest<Result<EnrollInCourseResponse>>;
