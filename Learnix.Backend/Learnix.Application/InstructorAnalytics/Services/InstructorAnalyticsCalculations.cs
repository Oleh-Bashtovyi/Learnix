using Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;
using Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.InstructorAnalytics.Services;

/// <summary>
/// Pure projections shared by the per-chart analytics handlers and the aggregate <c>overview</c> handler,
/// so the (fixed) formulas live in exactly one place.
/// </summary>
public static class InstructorAnalyticsCalculations
{
    /// <summary>
    /// Average rating across the instructor's courses, weighted by each course's review count — a course
    /// with one 5★ review no longer counts the same as one with hundreds of reviews.
    /// </summary>
    public static double WeightedAverageRating(IEnumerable<Course> courses)
    {
        var reviewed = courses.Where(c => c.ReviewsCount > 0).ToList();
        var totalReviews = reviewed.Sum(c => c.ReviewsCount);

        if (totalReviews == 0)
            return 0;

        var weightedSum = reviewed.Sum(c => (double)c.AverageRating * c.ReviewsCount);
        return Math.Round(weightedSum / totalReviews, 2);
    }

    public static CourseStatusesDto Statuses(IReadOnlyCollection<Course> courses) => new(
        courses.Count(c => c.Status == CourseStatus.Draft),
        courses.Count(c => c.Status == CourseStatus.Published),
        courses.Count(c => c.Status == CourseStatus.Archived));

    public static List<CoursePopularityItemDto> Popularity(IEnumerable<Course> courses) => courses
        .OrderByDescending(c => c.EnrollmentsCount)
        .Select(c => new CoursePopularityItemDto(c.Id, c.Title, c.EnrollmentsCount))
        .ToList();

    public static InstructorRatingDistributionDto Distribution(IReadOnlyDictionary<int, int> countsByRating) => new(
        countsByRating.GetValueOrDefault(1),
        countsByRating.GetValueOrDefault(2),
        countsByRating.GetValueOrDefault(3),
        countsByRating.GetValueOrDefault(4),
        countsByRating.GetValueOrDefault(5));
}
