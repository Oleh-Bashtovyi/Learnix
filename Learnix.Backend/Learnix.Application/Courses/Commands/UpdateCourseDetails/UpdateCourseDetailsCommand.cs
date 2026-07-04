using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed record UpdateCourseDetailsCommand(
    Guid CourseId,
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string? CoverImageUrl,
    IEnumerable<string> Tags) : IRequest<Result>, ICommandWithCourseId;
