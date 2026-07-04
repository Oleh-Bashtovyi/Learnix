using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminDeleteCourse;

public sealed record AdminDeleteCourseCommand(Guid CourseId) : IRequest<Result>;
