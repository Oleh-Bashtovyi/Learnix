using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Constants;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Services;

/// <summary>
/// The window-over-window part of the summary: what the last
/// <see cref="InstructorAnalyticsConstants.SummaryTrendWindowDays"/> days added, and how that
/// compares with the equally long window before it.
/// </summary>
/// <remarks>
/// Only flow metrics get a trend. Course count moves when the instructor publishes and average
/// rating is a weighted lifetime figure — a percentage against either says nothing about
/// performance, so neither is measured here.
/// </remarks>
public static class InstructorSummaryTrends
{
    public sealed record Result(
        InstructorAnalyticsTrendDto NewStudents,
        InstructorAnalyticsTrendDto Revenue,
        InstructorAnalyticsTrendDto Certificates);

    public static async Task<Result> LoadAsync(
        Guid instructorId,
        DateTime nowUtc,
        IEnrollmentRepository enrollmentRepository,
        IPaymentRepository paymentRepository,
        ICertificateRepository certificateRepository,
        CancellationToken cancellationToken)
    {
        var days = InstructorAnalyticsConstants.SummaryTrendWindowDays;

        // Whole days, so the windows line up with the daily buckets the earnings query returns and a
        // request at 09:00 measures the same span as one at 23:00.
        var today = nowUtc.Date;
        var currentStart = today.AddDays(-(days - 1));
        var previousStart = currentStart.AddDays(-days);
        var currentEnd = today.AddDays(1).AddTicks(-1);
        var previousEnd = currentStart.AddTicks(-1);

        var newStudentsCurrent = await enrollmentRepository.CountNewStudentsAsync(
            instructorId, currentStart, currentEnd, cancellationToken);
        var newStudentsPrevious = await enrollmentRepository.CountNewStudentsAsync(
            instructorId, previousStart, previousEnd, cancellationToken);

        // One query for both windows: the daily buckets are split on the client side of the database
        // call rather than paying for a second round trip over the same rows.
        var dailyEarnings = await paymentRepository.GetDailyEarningsAsync(
            instructorId, previousStart, currentEnd, cancellationToken);
        var revenueCurrent = dailyEarnings.Where(b => b.Key >= currentStart).Sum(b => b.Value);
        var revenuePrevious = dailyEarnings.Where(b => b.Key < currentStart).Sum(b => b.Value);

        var certificatesCurrent = await certificateRepository.CountAsync(
            new InstructorCertificatesInPeriodSpecification(instructorId, currentStart, currentEnd),
            cancellationToken);
        var certificatesPrevious = await certificateRepository.CountAsync(
            new InstructorCertificatesInPeriodSpecification(instructorId, previousStart, previousEnd),
            cancellationToken);

        return new Result(
            Trend(newStudentsCurrent, newStudentsPrevious),
            Trend(revenueCurrent, revenuePrevious),
            Trend(certificatesCurrent, certificatesPrevious));
    }

    private static InstructorAnalyticsTrendDto Trend(decimal current, decimal previous)
    {
        var changePercent = previous == 0m
            ? (double?)null
            : Math.Round((double)((current - previous) / previous) * 100, 1);

        return new InstructorAnalyticsTrendDto(current, changePercent);
    }
}
