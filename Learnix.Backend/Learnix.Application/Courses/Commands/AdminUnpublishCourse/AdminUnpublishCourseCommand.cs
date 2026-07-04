using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminUnpublishCourse;

public sealed record AdminUnpublishCourseCommand(Guid CourseId) : IRequest<Result>;
