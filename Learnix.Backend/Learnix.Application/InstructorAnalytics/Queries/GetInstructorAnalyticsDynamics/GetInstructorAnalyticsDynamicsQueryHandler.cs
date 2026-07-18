using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public sealed class GetInstructorAnalyticsDynamicsQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    IPaymentRepository paymentRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorAnalyticsDynamicsQuery, List<InstructorAnalyticsDynamicsItemDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorAnalyticsDynamicsItemDto>>> HandleAsync(
        GetInstructorAnalyticsDynamicsQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        // The dates arrive from query params as Kind=Unspecified, which Npgsql rejects against a
        // 'timestamp with time zone' column. Treat them as UTC calendar days, and stretch the end to the
        // last instant of its day so the final day's activity is included, not cut off at midnight.
        var startUtc = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);

        var enrollments = await enrollmentRepository.ListAsync(
            new InstructorEnrollmentsByDateSpecification(instructorId, startUtc, endUtc),
            cancellationToken);

        var payments = await paymentRepository.ListAsync(
            new InstructorPaymentsByDateSpecification(instructorId, startUtc, endUtc),
            cancellationToken);

        // Group by day
        var enrollmentGroups = enrollments
            .GroupBy(e => e.EnrolledAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var paymentGroups = payments
            .GroupBy(p => p.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        // Create a continuous list of dates from StartDate to EndDate
        var result = new List<InstructorAnalyticsDynamicsItemDto>();
        for (var date = startUtc.Date; date <= endUtc.Date; date = date.AddDays(1))
        {
            var dailyEnrollments = enrollmentGroups.GetValueOrDefault(date, 0);
            var dailyEarnings = paymentGroups.GetValueOrDefault(date, 0m);

            result.Add(new InstructorAnalyticsDynamicsItemDto(
                date.ToString("yyyy-MM-dd"),
                dailyEnrollments,
                dailyEarnings));
        }

        return Result.Ok(result);
    }
}
