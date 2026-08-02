using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
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

        var courseIds = courses.Select(c => c.Id).ToList();

        // A CourseId filter that isn't one of the instructor's own courses is a resource-authorization
        // failure, not an empty result — matches GetInstructorLessonDropOffQueryHandler.
        if (request.CourseId is { } courseId)
        {
            if (!courseIds.Contains(courseId))
                return Result.Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

            courseIds = [courseId];
        }

        if (courseIds.Count == 0)
            return Result.Ok(new InstructorRatingDistributionDto(0, 0, 0, 0, 0));

        var counts = await reviewRepository.GetRatingDistributionAsync(courseIds, cancellationToken);

        return Result.Ok(InstructorAnalyticsCalculations.Distribution(counts));
    }
}
