using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;

public sealed class GetInstructorRecentReviewsQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorRecentReviewsQuery, List<InstructorRecentReviewDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorRecentReviewDto>>> HandleAsync(
        GetInstructorRecentReviewsQuery request, Guid instructorId, CancellationToken cancellationToken)
    {

        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        if (courses.Count == 0)
            return Result.Ok(new List<InstructorRecentReviewDto>());

        // Narrowing to a course the instructor does not own yields an empty id set → no reviews.
        var courseIds = courses
            .Select(c => c.Id)
            .Where(id => request.CourseId is null || id == request.CourseId)
            .ToList();

        var reviews = await reviewRepository.ListAsync(
            new InstructorReviewsSpecification(courseIds, request.Take),
            cancellationToken);

        var result = reviews
            .Select(r => new InstructorRecentReviewDto(
                r.CourseId,
                courses.First(c => c.Id == r.CourseId).Title,
                r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "Unknown Student",
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToList();

        return Result.Ok(result);
    }
}
