using FluentResults;
using MediatR;

namespace Learnix.Application.Sections.Commands.DeleteSection;

public sealed record DeleteSectionCommand(Guid CourseId, Guid SectionId) : IRequest<Result>;
