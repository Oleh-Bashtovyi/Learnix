using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminPublishCourse;

public sealed record AdminPublishCourseCommand(Guid CourseId) : IRequest<Result>;
