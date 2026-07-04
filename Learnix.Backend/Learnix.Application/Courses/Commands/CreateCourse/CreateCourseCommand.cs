using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.CreateCourse;

public sealed record CreateCourseCommand(
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    IEnumerable<string>? Tags) : IRequest<Result<CreateCourseResponse>>;
