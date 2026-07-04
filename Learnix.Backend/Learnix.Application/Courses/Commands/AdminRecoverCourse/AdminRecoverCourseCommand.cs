using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminRecoverCourse;

public sealed record AdminRecoverCourseCommand(Guid CourseId) : IRequest<Result>;
