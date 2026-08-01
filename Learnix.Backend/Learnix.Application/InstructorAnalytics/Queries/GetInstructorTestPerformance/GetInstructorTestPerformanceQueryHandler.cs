using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
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
        // includeSections so each test lesson's title is available in memory for the projection below.
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId, includeSections: true),
            cancellationToken);

        if (courses.Count == 0)
            return Result.Ok(new List<InstructorTestPerformanceDto>());

        var courseIds = courses.Select(c => c.Id).ToList();

        var buckets = await testAttemptRepository.GetPerformanceByTestAsync(courseIds, cancellationToken);

        var result = buckets.Select(b =>
        {
            var course = courses.First(c => c.Id == b.CourseId);

            var lessonTitle = course.Sections
                .SelectMany(s => s.Lessons)
                .FirstOrDefault(l => l.Id == b.TestLessonId)?.Title ?? "Test Lesson";

            // All attempts in a bucket are for the same test, so they share a max score. Exposing it lets
            // the client render "7 / 10" and derive a percentage — the raw average alone is meaningless.
            var passRate = (double)b.PassedCount / b.TotalAttempts;

            return new InstructorTestPerformanceDto(
                course.Id,
                course.Title,
                b.TestLessonId,
                lessonTitle,
                Math.Round(b.AverageScore, 2),
                b.MaxScore,
                Math.Round(passRate, 2));
        }).ToList();

        return Result.Ok(result);
    }
}
