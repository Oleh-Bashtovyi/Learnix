using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Services;

public class InstructorAnalyticsCalculationsTests
{
    private static Course CourseWithRating(int reviewsCount, decimal averageRating)
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "C", "d", 0m);
        course.SyncRating(reviewsCount, averageRating);
        return course;
    }

    [Fact]
    public void WeightedAverageRating_ShouldWeightByReviewCount_NotAverageTheAverages()
    {
        // Arrange — a lone 5★ review must not count the same as three 3★ reviews
        var courses = new List<Course>
        {
            CourseWithRating(reviewsCount: 1, averageRating: 5m),
            CourseWithRating(reviewsCount: 3, averageRating: 3m),
        };

        // Act
        var result = InstructorAnalyticsCalculations.WeightedAverageRating(courses);

        // Assert — (5*1 + 3*3) / 4 = 3.5, not the unweighted mean of 4.0
        result.Should().Be(3.5);
    }

    [Fact]
    public void WeightedAverageRating_WhenNoCourseHasReviews_ShouldBeZero()
    {
        var courses = new List<Course> { CourseWithRating(reviewsCount: 0, averageRating: 0m) };

        var result = InstructorAnalyticsCalculations.WeightedAverageRating(courses);

        result.Should().Be(0);
    }

    [Fact]
    public void Distribution_ShouldMapCountsAndDefaultMissingRatingsToZero()
    {
        // Arrange — only 3★ and 5★ have reviews
        var counts = new Dictionary<int, int> { [5] = 2, [3] = 1 };

        // Act
        var result = InstructorAnalyticsCalculations.Distribution(counts);

        // Assert
        result.OneStar.Should().Be(0);
        result.TwoStar.Should().Be(0);
        result.ThreeStar.Should().Be(1);
        result.FourStar.Should().Be(0);
        result.FiveStar.Should().Be(2);
    }
}
