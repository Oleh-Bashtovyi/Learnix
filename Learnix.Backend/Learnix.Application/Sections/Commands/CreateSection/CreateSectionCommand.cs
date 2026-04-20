using FluentResults;
using MediatR;

namespace Learnix.Application.Sections.Commands.CreateSection;

public sealed record CreateSectionCommand(Guid CourseId, string Title) : IRequest<Result<Guid>>;
