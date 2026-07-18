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

        var attempts = await testAttemptRepository.ListAsync(
            new InstructorTestAttemptsSpecification(courseIds),
            cancellationToken);

        if (attempts.Count == 0)
            return Result.Ok(new List<InstructorTestPerformanceDto>());

        // Group by CourseId and TestLessonId
        var groups = attempts.GroupBy(a => new { a.CourseId, a.TestLessonId });

        var result = new List<InstructorTestPerformanceDto>();

        foreach (var g in groups)
        {
            var course = courses.First(c => c.Id == g.Key.CourseId);

            var lessonTitle = course.Sections
                .SelectMany(s => s.Lessons)
                .FirstOrDefault(l => l.Id == g.Key.TestLessonId)?.Title ?? "Test Lesson";

            var totalAttempts = g.Count();
            var averageScore = g.Average(a => a.Score ?? 0);
            // All attempts in a group are for the same test, so they share a max score. Exposing it lets
            // the client render "7 / 10" and derive a percentage — the raw average alone is meaningless.
            var maxScore = g.Max(a => a.MaxScore ?? 0);
            var passRate = (double)g.Count(a => a.Passed == true) / totalAttempts;

            result.Add(new InstructorTestPerformanceDto(
                course.Id,
                course.Title,
                g.Key.TestLessonId,
                lessonTitle,
                Math.Round(averageScore, 2),
                maxScore,
                Math.Round(passRate, 2)));
        }

        return Result.Ok(result);
    }
}
