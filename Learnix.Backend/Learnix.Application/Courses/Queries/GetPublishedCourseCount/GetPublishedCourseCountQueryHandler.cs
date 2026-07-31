using FluentResults;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublishedCourseCount;

internal sealed class GetPublishedCourseCountQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetPublishedCourseCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetPublishedCourseCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await courseRepository.CountAsync(
            new AdminCoursesByStatusCountSpecification(CourseStatus.Published), cancellationToken);

        return Result.Ok(count);
    }
}
