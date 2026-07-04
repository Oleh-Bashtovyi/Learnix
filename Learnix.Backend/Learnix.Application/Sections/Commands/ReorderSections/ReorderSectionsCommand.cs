using FluentResults;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Models;
using MediatR;

namespace Learnix.Application.Sections.Commands.ReorderSections;

public sealed record ReorderSectionsCommand(
    Guid CourseId,
    IReadOnlyList<ReorderItem> Items) : IRequest<Result>, ICommandWithCourseId;
