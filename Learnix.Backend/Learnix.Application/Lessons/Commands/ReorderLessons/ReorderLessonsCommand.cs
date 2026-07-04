using FluentResults;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Models;
using MediatR;

namespace Learnix.Application.Lessons.Commands.ReorderLessons;

public sealed record ReorderLessonsCommand(
    Guid CourseId,
    Guid SectionId,
    IReadOnlyList<ReorderItem> Items) : IRequest<Result>, ICommandWithCourseAndSectionId;
