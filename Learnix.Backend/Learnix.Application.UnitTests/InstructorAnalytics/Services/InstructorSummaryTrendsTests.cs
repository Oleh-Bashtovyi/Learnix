using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Constants;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.Payments.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Services;

public class InstructorSummaryTrendsTests
{
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();

    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly DateTime NowUtc = new(2026, 3, 31, 14, 0, 0, DateTimeKind.Utc);
    private static readonly int WindowDays = InstructorAnalyticsConstants.SummaryTrendWindowDays;

    private Task<InstructorSummaryTrends.Result> LoadAsync() =>
        InstructorSummaryTrends.LoadAsync(
            InstructorId, NowUtc, _enrollmentRepository, _paymentRepository, _certificateRepository, CancellationToken.None);

    [Fact]
    public async Task Revenue_is_split_into_the_two_windows_by_bucket_date()
    {
        var today = NowUtc.Date;
        _paymentRepository
            .GetDailyEarningsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<DateTime, decimal>
            {
                [today] = 300m,                                 // current window
                [today.AddDays(-(WindowDays - 1))] = 100m,      // first day of the current window
                [today.AddDays(-WindowDays)] = 200m,            // last day of the previous window
            });

        var result = await LoadAsync();

        result.Revenue.Current.Should().Be(400m);
        result.Revenue.ChangePercent.Should().Be(100);
    }

    [Fact]
    public async Task Change_is_null_when_the_previous_window_is_empty()
    {
        _enrollmentRepository
            .CountNewStudentsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<DateTime>(1) >= NowUtc.Date.AddDays(-(WindowDays - 1)) ? 5 : 0);

        var result = await LoadAsync();

        result.NewStudents.Current.Should().Be(5);
        result.NewStudents.ChangePercent.Should().BeNull();
    }

    [Fact]
    public async Task A_drop_to_nothing_is_reported_as_minus_one_hundred_percent()
    {
        _certificateRepository
            .CountAsync(Arg.Any<ISpecification<Certificate>>(), Arg.Any<CancellationToken>())
            .Returns(0, 4);

        var result = await LoadAsync();

        result.Certificates.Current.Should().Be(0);
        result.Certificates.ChangePercent.Should().Be(-100);
    }

    [Fact]
    public async Task The_two_windows_are_equal_in_length_and_do_not_overlap()
    {
        var starts = new List<DateTime>();
        var ends = new List<DateTime>();
        _enrollmentRepository
            .CountNewStudentsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                starts.Add(call.ArgAt<DateTime>(1));
                ends.Add(call.ArgAt<DateTime>(2));
                return 0;
            });

        await LoadAsync();

        var (currentStart, currentEnd) = (starts[0], ends[0]);
        var (previousStart, previousEnd) = (starts[1], ends[1]);

        currentStart.Should().Be(NowUtc.Date.AddDays(-(WindowDays - 1)));
        previousStart.Should().Be(currentStart.AddDays(-WindowDays));
        previousEnd.Should().BeBefore(currentStart);
        (currentEnd - currentStart).Should().Be(previousEnd - previousStart);
    }
}
