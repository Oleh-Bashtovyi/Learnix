using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Payments.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class PaymentRepository(ApplicationDbContext context)
    : RepositoryBase<Payment>(context), IPaymentRepository
{
    public Task<decimal> GetTotalEarningsAsync(Guid instructorId, CancellationToken cancellationToken = default)
        => context.Payments
            .Where(p => p.Status == PaymentStatus.Completed && p.Course!.InstructorId == instructorId)
            .SumAsync(p => p.Amount, cancellationToken);

    public async Task<IReadOnlyDictionary<DateTime, decimal>> GetDailyEarningsAsync(
        Guid instructorId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        var buckets = await context.Payments
            .Where(p => p.Status == PaymentStatus.Completed && p.Course!.InstructorId == instructorId &&
                        p.CreatedAt >= startUtc && p.CreatedAt <= endUtc)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        return buckets.ToDictionary(b => b.Date, b => b.Total);
    }
}
