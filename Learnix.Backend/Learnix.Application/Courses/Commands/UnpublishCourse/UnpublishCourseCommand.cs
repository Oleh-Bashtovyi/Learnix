using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Courses.Commands.UnpublishCourse;

public sealed record UnpublishCourseCommand(Guid CourseId) : IRequest<Result>, ICommandWithCourseId;
