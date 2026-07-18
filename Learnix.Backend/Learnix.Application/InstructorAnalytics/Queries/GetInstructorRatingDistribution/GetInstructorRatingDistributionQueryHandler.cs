using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;

public sealed class GetInstructorRatingDistributionQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorRatingDistributionQuery, InstructorRatingDistributionDto>(currentUser)
{
    protected override async Task<Result<InstructorRatingDistributionDto>> HandleAsync(
        GetInstructorRatingDistributionQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        if (courses.Count == 0)
            return Result.Ok(new InstructorRatingDistributionDto(0, 0, 0, 0, 0));

        // Narrowing to a course the instructor does not own yields an empty id set → all-zero result.
        var courseIds = courses
            .Select(c => c.Id)
            .Where(id => request.CourseId is null || id == request.CourseId)
            .ToList();

        var counts = await reviewRepository.GetRatingDistributionAsync(courseIds, cancellationToken);

        return Result.Ok(InstructorAnalyticsCalculations.Distribution(counts));
    }
}
