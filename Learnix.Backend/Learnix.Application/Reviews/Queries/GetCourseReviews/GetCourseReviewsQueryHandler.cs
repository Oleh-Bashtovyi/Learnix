using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Specifications;
using MediatR;

namespace Learnix.Application.Reviews.Queries.GetCourseReviews;

public sealed class GetCourseReviewsQueryHandler(
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetCourseReviewsQuery, Result<PaginatedResult<CourseReviewDto>>>
{
    public async Task<Result<PaginatedResult<CourseReviewDto>>> Handle(
        GetCourseReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await reviewRepository.CountAsync(
            new CourseReviewsByCoursePaginatedCountSpecification(request.CourseId), cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<CourseReviewDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var reviews = await reviewRepository.ListAsync(
            new CourseReviewsByCoursePaginatedSpecification(request.CourseId, pagination.Skip, pagination.Take),
            cancellationToken);

        var dtos = reviews.Select(r => new CourseReviewDto(
            r.Id,
            r.StudentId,
            r.Student!.FirstName,
            r.Student.LastName,
            !string.IsNullOrWhiteSpace(r.Student.AvatarBlobPath) ? blobStorage.GetPublicUrl(r.Student.AvatarBlobPath) : null,
            r.Rating,
            r.Comment,
            r.CreatedAt,
            r.UpdatedAt));

        return Result.Ok(PaginatedResult<CourseReviewDto>.Create(
            dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}
