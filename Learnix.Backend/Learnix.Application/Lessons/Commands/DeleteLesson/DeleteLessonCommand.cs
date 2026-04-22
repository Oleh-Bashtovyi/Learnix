using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Lessons.Commands.DeleteLesson;

public sealed record DeleteLessonCommand(Guid CourseId, Guid LessonId) : IRequest<Result>, ICommandWithCourseId;
