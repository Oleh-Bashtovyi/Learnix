using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingTrend;

public sealed class GetInstructorRatingTrendQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorRatingTrendQuery, List<InstructorRatingTrendItemDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorRatingTrendItemDto>>> HandleAsync(
        GetInstructorRatingTrendQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);

        var courseIds = courses.Select(c => c.Id).ToList();

        // A CourseId filter that isn't one of the instructor's own courses is a resource-authorization
        // failure, not an empty result — matches GetInstructorLessonDropOffQueryHandler.
        if (request.CourseId is { } courseId)
        {
            if (!courseIds.Contains(courseId))
                return Result.Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

            courseIds = [courseId];
        }

        var buckets = await reviewRepository.GetMonthlyRatingTrendAsync(courseIds, cancellationToken);

        var result = buckets
            .OrderBy(b => b.Year).ThenBy(b => b.Month)
            .Select(b => new InstructorRatingTrendItemDto(
                $"{b.Year:D4}-{b.Month:D2}",
                Math.Round(b.Average, 2),
                b.Count))
            .ToList();

        return Result.Ok(result);
    }
}
