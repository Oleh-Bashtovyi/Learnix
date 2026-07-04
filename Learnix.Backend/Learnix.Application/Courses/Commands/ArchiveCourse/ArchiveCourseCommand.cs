using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Courses.Commands.ArchiveCourse;

public sealed record ArchiveCourseCommand(Guid CourseId) : IRequest<Result>, ICommandWithCourseId;
