using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.TestAttempts.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorTestPerformance;

public sealed class GetInstructorTestPerformanceQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ITestAttemptRepository testAttemptRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorTestPerformanceQuery, List<InstructorTestPerformanceDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorTestPerformanceDto>>> HandleAsync(
        GetInstructorTestPerformanceQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var ownedCourses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);

        if (ownedCourses.Count == 0)
            return Result.Ok(new List<InstructorTestPerformanceDto>());

        var courseIds = ownedCourses.Select(c => c.Id).ToList();

        // A CourseId filter that isn't one of the instructor's own courses is a resource-authorization
        // failure, not an empty result — matches GetInstructorRatingDistributionQueryHandler.
        if (request.CourseId is { } courseId)
        {
            if (!courseIds.Contains(courseId))
                return Result.Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

            courseIds = [courseId];
        }

        var buckets = await testAttemptRepository.GetPerformanceByTestAsync(courseIds, cancellationToken);

        if (buckets.Count == 0)
            return Result.Ok(new List<InstructorTestPerformanceDto>());

        // Sections/lessons are loaded only for the courses that actually turned up a bucket — not
        // every course the instructor owns — since all they're needed for is the lesson title below.
        var bucketCourseIds = buckets.Select(b => b.CourseId).Distinct().ToList();
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId, includeSections: true, courseIds: bucketCourseIds),
            cancellationToken);

        var result = buckets.Select(b =>
        {
            var course = courses.First(c => c.Id == b.CourseId);

            var lessonTitle = course.Sections
                .SelectMany(s => s.Lessons)
                .FirstOrDefault(l => l.Id == b.TestLessonId)?.Title ?? "Test Lesson";

            // Every attempt in a bucket shares a max score (see GetPerformanceByTestAsync), so exposing
            // it alongside the average lets the client render "7 / 10" and derive a percentage.
            var passRate = (double)b.PassedCount / b.TotalAttempts;

            return new InstructorTestPerformanceDto(
                course.Id,
                course.Title,
                b.TestLessonId,
                lessonTitle,
                Math.Round(b.AverageScore, 2),
                b.MaxScore,
                Math.Round(passRate, 2),
                b.TotalAttempts);
        }).ToList();

        return Result.Ok(result);
    }
}
