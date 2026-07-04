using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Courses.Commands.DeleteCourse;

public sealed record DeleteCourseCommand(Guid CourseId) : IRequest<Result>, ICommandWithCourseId;
