using FluentResults;
using MediatR;

namespace Learnix.Application.Sections.Commands.UpdateSectionTitle;

public sealed record UpdateSectionTitleCommand(Guid CourseId, Guid SectionId, string Title)
    : IRequest<Result>;
