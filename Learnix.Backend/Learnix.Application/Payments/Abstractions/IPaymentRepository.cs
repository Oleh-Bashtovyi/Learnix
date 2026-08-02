using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Payments.Abstractions;

public interface IPaymentRepository : IRepositoryBase<Payment>
{
    /// <summary>
    /// Total completed-payment revenue across the instructor's courses.
    /// </summary>
    Task<decimal> GetTotalEarningsAsync(Guid instructorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed-payment revenue per calendar day (UTC) across the instructor's courses, within
    /// <paramref name="startUtc"/>–<paramref name="endUtc"/> inclusive.
    /// </summary>
    Task<IReadOnlyDictionary<DateTime, decimal>> GetDailyEarningsAsync(
        Guid instructorId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
