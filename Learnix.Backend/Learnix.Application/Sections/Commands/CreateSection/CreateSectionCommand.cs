using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Sections.Commands.CreateSection;

public sealed record CreateSectionCommand(Guid CourseId, string Title)
    : IRequest<Result<Guid>>, ICommandWithCourseId;
