using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseForEditById;

public sealed record GetCourseForEditByIdQuery(Guid CourseId) : IRequest<Result<CourseForEditDto>>;
