using Learnix.Application.InstructorAnalytics.Constants;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public class GetInstructorAnalyticsDynamicsQueryValidatorTests
{
    private readonly GetInstructorAnalyticsDynamicsQueryValidator _sut = new();

    [Fact]
    public void Validate_WhenDatesAreMissing_ShouldFail()
    {
        // Arrange — the value bound when the query params are absent: default(DateTime), which would
        // otherwise drive the per-day loop through ~740k iterations
        var query = new GetInstructorAnalyticsDynamicsQuery(default, default);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenEndIsBeforeStart_ShouldFail()
    {
        var query = new GetInstructorAnalyticsDynamicsQuery(
            new DateTime(2026, 06, 10), new DateTime(2026, 06, 01));

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenRangeExceedsTheMaximum_ShouldFail()
    {
        var start = new DateTime(2026, 01, 01);
        var query = new GetInstructorAnalyticsDynamicsQuery(
            start, start.AddDays(InstructorAnalyticsConstants.MaxDynamicsRangeDays + 1));

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithAValidRange_ShouldPass()
    {
        var query = new GetInstructorAnalyticsDynamicsQuery(
            new DateTime(2026, 06, 01), new DateTime(2026, 06, 30));

        var result = _sut.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
