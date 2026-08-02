using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Models;
using Learnix.Application.Payments.Specifications;

namespace Learnix.Application.Payments.Services;

/// <summary>
/// Shared by <c>GetMyEarnings</c> (the instructor's own view) and <c>GetInstructorEarnings</c> (the
/// admin's view of a specific instructor) — same computation, two different sources for whose id it is.
/// </summary>
public static class InstructorEarnings
{
    public static async Task<InstructorEarningsResponse> ComputeAsync(
        IPaymentRepository paymentRepository, Guid instructorId, CancellationToken cancellationToken)
    {
        var payments = await paymentRepository.ListAsync(
            new InstructorPaymentsSpecification(instructorId),
            cancellationToken);

        if (payments.Count == 0)
            return new InstructorEarningsResponse(0m, 0, []);

        var courses = payments
            .GroupBy(p => p.CourseId)
            .Select(g => new CourseEarningsDto(
                g.Key,
                g.First().Course?.Title ?? string.Empty,
                g.Count(),
                g.Sum(p => p.Amount),
                g.Max(p => p.CreatedAt)))
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var totalEarnings = courses.Sum(c => c.TotalAmount);
        var totalPayments = courses.Sum(c => c.PaymentsCount);

        return new InstructorEarningsResponse(totalEarnings, totalPayments, courses);
    }
}
