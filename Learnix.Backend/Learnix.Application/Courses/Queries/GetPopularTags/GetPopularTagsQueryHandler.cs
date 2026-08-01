using FluentResults;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPopularTags;

public sealed class GetPopularTagsQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetPopularTagsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetPopularTagsQuery request,
        CancellationToken cancellationToken)
    {
        var tags = await courseRepository.GetPopularTagsAsync(
            request.CategoryId,
            CourseTagConstants.PopularTagsLimit,
            CourseTagConstants.PopularTagsMinCourses,
            cancellationToken);

        return Result.Ok(tags);
    }
}
