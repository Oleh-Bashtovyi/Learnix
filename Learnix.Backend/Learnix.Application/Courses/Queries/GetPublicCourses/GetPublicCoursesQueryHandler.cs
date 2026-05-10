using FluentResults;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

internal sealed class GetPublicCoursesQueryHandler(IPublicCourseCatalogSearchService searchService)
    : IRequestHandler<GetPublicCoursesQuery, Result<PaginatedResult<PublicCourseCardDto>>>
{
    public async Task<Result<PaginatedResult<PublicCourseCardDto>>> Handle(
        GetPublicCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var result = await searchService.SearchAsync(
            request.Search,
            pagination,
            request.CategoryId,
            request.InstructorId,
            request.SortBy,
            request.IsFree,
            request.MinRating,
            cancellationToken);

        return Result.Ok(result);
    }
}
